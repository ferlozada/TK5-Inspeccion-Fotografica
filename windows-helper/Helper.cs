using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Xml;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

// Framework 4.8, no admin, no URL ACL, no dependencies. Never handles project data.
class Helper {
 const int Port=18765, Max=256*1024*1024;
 const string Origin="https://ferlozada.github.io";
 static string token, root;
 [STAThread] static void Main(string[] args) {
  root=args.Length==2 && args[0]=="--test-no-open" ? Path.GetFullPath(args[1]) : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"TK5PowerPoint");
  token=File.ReadAllText(Path.Combine(root,"token.txt")).Trim();
  if(token.Length!=64) return;
  using(var mutex=new Mutex(false,"Local\\TK5PowerPointHelper")) {
   if(!mutex.WaitOne(0)) return;
   var server=new TcpListener(IPAddress.Loopback,Port);
   try { server.Start(8); } catch { return; }
   while(true) using(var client=server.AcceptTcpClient()) {
    client.ReceiveTimeout=15000; client.SendTimeout=15000;
    try { Handle(client.GetStream(),args.Length>=1 && args[0]=="--test-no-open"); }
    catch { /* malformed/disconnected clients cannot terminate the helper */ }
    // Deliver rejection responses even when a small request body is still buffered.
    try {client.Client.Shutdown(SocketShutdown.Send);var drain=new byte[8192];int n=0;
     while(client.Available>0 && n<65536)n+=client.GetStream().Read(drain,0,Math.Min(drain.Length,client.Available));
    }catch { }
   }
  }
 }
 static bool Equal(string a,string b) {
  if(a==null || a.Length!=b.Length) return false;
  int n=0;for(int i=0;i<a.Length;i++) n|=a[i]^b[i];return n==0;
 }
 static void Reply(Stream s,int code,string body,bool cors) {
  byte[] b=Encoding.UTF8.GetBytes(body);
  string h="HTTP/1.1 "+code+" Result\r\nConnection: close\r\nContent-Type: application/json\r\nCache-Control: no-store\r\nContent-Length: "+b.Length+"\r\n";
  if(cors) h+="Access-Control-Allow-Origin: "+Origin+"\r\nVary: Origin\r\nAccess-Control-Allow-Methods: POST, OPTIONS\r\nAccess-Control-Allow-Headers: content-type, x-tk5-token\r\nAccess-Control-Allow-Private-Network: true\r\n";
  byte[] head=Encoding.ASCII.GetBytes(h+"\r\n");s.Write(head,0,head.Length);s.Write(b,0,b.Length);
 }
 static void Handle(Stream s,bool test) {
  var clock=Stopwatch.StartNew();
  var header=new List<byte>(); int c;
  while(header.Count<8192 && (c=s.ReadByte())>=0) {
   if(clock.ElapsedMilliseconds>15000)return;
   header.Add((byte)c);int n=header.Count;
   if(n>=4 && header[n-4]==13 && header[n-3]==10 && header[n-2]==13 && header[n-1]==10) break;
  }
  string raw=Encoding.ASCII.GetString(header.ToArray());
  if(!raw.EndsWith("\r\n\r\n")) {Reply(s,400,"{}",false);return;}
  string[] lines=raw.Split(new[]{"\r\n"},StringSplitOptions.None);
  var h=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
  for(int i=1;i<lines.Length;i++) {
   if(lines[i]=="") continue;int colon=lines[i].IndexOf(':');
   if(colon<1 || h.ContainsKey(lines[i].Substring(0,colon))) {Reply(s,400,"{}",false);return;}
   h.Add(lines[i].Substring(0,colon),lines[i].Substring(colon+1).Trim());
  }
  string origin,host;bool cors=h.TryGetValue("Origin",out origin) && origin==Origin;
  if(!cors || !h.TryGetValue("Host",out host) || host!="127.0.0.1:"+Port) {Reply(s,403,"{}",false);return;}
  if(lines[0]=="OPTIONS /open HTTP/1.1") {Reply(s,204,"",true);return;}
  string auth,len,type;
  if(lines[0]!="POST /open HTTP/1.1") {Reply(s,404,"{}",true);return;}
  if(!h.TryGetValue("X-TK5-Token",out auth)||!Equal(auth,token)) {Reply(s,401,"{}",true);return;}
  int size;
  if(h.ContainsKey("Transfer-Encoding") || !h.TryGetValue("Content-Length",out len) || !int.TryParse(len,out size) || size<1 || size>Max) {Reply(s,413,"{}",true);return;}
  if(!h.TryGetValue("Content-Type",out type)||type!="application/vnd.openxmlformats-officedocument.presentationml.presentation") {Reply(s,415,"{}",true);return;}
  byte[] data=new byte[size];int pos=0;
  while(pos<size) {if(clock.ElapsedMilliseconds>90000)return;int read=s.Read(data,pos,Math.Min(65536,size-pos));if(read<=0)return;pos+=read;}
  try {Validate(data);}catch {Reply(s,422,"{\"error\":\"PPTX no permitido\"}",true);return;}
  string folder=Path.Combine(root,"Presentaciones");Directory.CreateDirectory(folder);
  string file=Path.Combine(folder,"TK5_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+Guid.NewGuid().ToString("N")+".pptx");
  try {using(var f=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None)) f.Write(data,0,data.Length);}
  catch {Reply(s,500,"{\"error\":\"No se pudo guardar\"}",true);return;}
  bool opened=false;
  if(!test) try {Open(file);opened=true;}catch { }
  Reply(s,200,"{\"saved\":true,\"launched\":"+(opened?"true":"false")+"}",true);
 }
 static XmlDocument Xml(ZipArchiveEntry entry) {
  var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=4*1024*1024};
  var doc=new XmlDocument{XmlResolver=null};
  using(var s=entry.Open()) using(var reader=XmlReader.Create(s,settings))doc.Load(reader);
  return doc;
 }
 static void Validate(byte[] data) {
  using(var ms=new MemoryStream(data)) using(var zip=new ZipArchive(ms,ZipArchiveMode.Read)) {
   if(zip.Entries.Count>10000)throw new Exception();
   long total=0;var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
   foreach(var e in zip.Entries) {
    string n=e.FullName;
    if(!names.Add(n)||n.Contains("..")||n.Contains("\\")||n.StartsWith("/")||n.Contains(":")||n.IndexOf("vba",StringComparison.OrdinalIgnoreCase)>=0||n.StartsWith("ppt/embeddings/",StringComparison.OrdinalIgnoreCase)||n.EndsWith(".exe",StringComparison.OrdinalIgnoreCase))throw new Exception();
    total+=e.Length;if(total>768L*1024*1024 || e.Length>Max)throw new Exception();
    if(n.EndsWith(".rels",StringComparison.OrdinalIgnoreCase)) {
     foreach(XmlNode node in Xml(e).GetElementsByTagName("Relationship"))
      if(node.Attributes["TargetMode"]!=null && node.Attributes["TargetMode"].Value.Equals("External",StringComparison.OrdinalIgnoreCase))throw new Exception();
    }
   }
   var ct=zip.GetEntry("[Content_Types].xml");var p=zip.GetEntry("ppt/presentation.xml");
   if(ct==null||p==null||zip.GetEntry("_rels/.rels")==null)throw new Exception();
   bool valid=false;
   foreach(XmlNode node in Xml(ct).GetElementsByTagName("Override")) {
    string type=node.Attributes["ContentType"].Value;
    if(type.IndexOf("macro",StringComparison.OrdinalIgnoreCase)>=0||type.IndexOf("oleObject",StringComparison.OrdinalIgnoreCase)>=0)throw new Exception();
    if(node.Attributes["PartName"].Value=="/ppt/presentation.xml" && type=="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml")valid=true;
   }
   var doc=Xml(p);
   if(!valid || doc.DocumentElement.LocalName!="presentation" || doc.DocumentElement.NamespaceURI!="http://schemas.openxmlformats.org/presentationml/2006/main")throw new Exception();
  }
 }
 static void Open(string file) {
  // Prefer actual PowerPoint; association is only used if its executable is PowerPoint.
  string exe=null;
  foreach(var hive in new[]{Registry.CurrentUser,Registry.LocalMachine})
   using(var key=hive.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\POWERPNT.EXE"))
    if(key!=null)exe=key.GetValue(null) as string ?? exe;
  if(exe!=null && File.Exists(exe.Trim('"'))) {
   Process.Start(new ProcessStartInfo(exe.Trim('"'),"\""+file+"\""){UseShellExecute=true});return;
  }
  Type t=Type.GetTypeFromProgID("PowerPoint.Application");if(t==null)throw new Exception();
  dynamic app=Activator.CreateInstance(t);app.Visible=-1;
  app.Presentations.Open(file,0,0,-1);
 }
}
