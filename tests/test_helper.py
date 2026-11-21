"""Usage: python test_helper.py <external-work-directory>. No project data used."""
import io, zipfile, subprocess, pathlib, sys, time, http.client, os
root = pathlib.Path(sys.argv[1]).resolve()
root.mkdir(parents=True, exist_ok=True)
repo = pathlib.Path(__file__).resolve().parents[1]
exe = root / 'TK5PowerPoint.exe'
subprocess.run([os.environ['WINDIR'] + r'\Microsoft.NET\Framework64\v4.0.30319\csc.exe', '/nologo', '/target:winexe', '/out:'+str(exe), '/r:System.IO.Compression.dll', '/r:Microsoft.CSharp.dll', str(repo/'windows-helper/Helper.cs')], check=True)
token = 'a'*64
(root/'token.txt').write_text(token)
p = subprocess.Popen([str(exe), '--test-no-open', str(root)])
def request(body=b'', method='POST', origin='https://ferlozada.github.io', auth=token, extra=None):
    c = http.client.HTTPConnection('127.0.0.1',18765,timeout=5)
    h={'Origin':origin,'X-TK5-Token':auth,'Content-Type':'application/vnd.openxmlformats-officedocument.presentationml.presentation'}
    h.update(extra or {})
    c.request(method,'/open',body,h)
    r=c.getresponse(); result=(r.status,dict(r.getheaders()),r.read());c.close();return result
def pptx(extra=None):
    b=io.BytesIO()
    with zipfile.ZipFile(b,'w',zipfile.ZIP_DEFLATED) as z:
        z.writestr('[Content_Types].xml','<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Override PartName="/ppt/presentation.xml" ContentType="application/vnd.openxmlformats-officedocument.presentationml.presentation.main+xml"/></Types>')
        z.writestr('ppt/presentation.xml','<p:presentation xmlns:p="http://schemas.openxmlformats.org/presentationml/2006/main"/>')
        z.writestr('_rels/.rels','<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"/>')
        if extra: z.writestr(*extra)
    return b.getvalue()
try:
    for _ in range(50):
        try: request(method='OPTIONS'); break
        except OSError: time.sleep(.1)
    assert request(method='OPTIONS')[0]==204
    bad=request(pptx(),origin='https://evil.example');assert bad[0]==403 and 'Access-Control-Allow-Origin' not in bad[1]
    assert request(pptx(),auth='b'*64)[0]==401
    assert request(b'not zip')[0]==422
    assert request(pptx(),extra={'Content-Type':'text/plain'})[0]==415
    assert request(pptx(),extra={'Content-Length':str(256*1024*1024+1)})[0]==413
    for entry in [('.. /bad.exe','x'),('ppt/vbaProject.bin','x'),('ppt/embeddings/oleObject.bin','x'),('ppt/_rels/presentation.xml.rels','<Relationships><Relationship TargetMode="External" Target="http://evil"/></Relationships>')]:
        assert request(pptx(entry))[0]==422,entry
    before=set((root/'Presentaciones').glob('*.pptx')) if (root/'Presentaciones').exists() else set()
    for _ in range(2):
        r=request(pptx());assert r[0]==200 and b'"launched":false' in r[2]
    after=set((root/'Presentaciones').glob('*.pptx'))
    assert len(after-before)==2
    assert all(f.read_bytes()==pptx() for f in after-before)
    print('PASS: CORS, token, size, MIME, ZIP, traversal, VBA, OLE, external links, unique writes, no-open mode')
finally:
    p.terminate();p.wait(timeout=5)
