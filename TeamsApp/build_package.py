import json, struct, zlib, zipfile
from pathlib import Path
root=Path(__file__).resolve().parent
def png(n, outline=False):
    def chunk(tag,data):
        return struct.pack(">I",len(data))+tag+data+struct.pack(">I",zlib.crc32(tag+data)&0xffffffff)
    rows=[]
    for y in range(n):
        row=bytearray()
        for x in range(n):
            # Symmetrical audio bars, centered with transparent outline.
            u,v=(x+.5)/n,(y+.5)/n
            heights=(.16,.30,.48,.30,.16)
            mark=any(abs(u-(.26+i*.12))<.038 and abs(v-.5)<h/2 for i,h in enumerate(heights))
            row.extend((255,255,255,255) if mark else ((0,0,0,0) if outline else (226,0,116,255)))
        rows.append(b"\0"+row)
    return b"\x89PNG\r\n\x1a\n"+chunk(b"IHDR",struct.pack(">IIBBBBB",n,n,8,6,0,0,0))+chunk(b"IDAT",zlib.compress(b"".join(rows)))+chunk(b"IEND",b"")
manifest=(root/"manifest.json").read_bytes()
json.loads(manifest)
with zipfile.ZipFile(root/"ht-voice-teams.zip","w",zipfile.ZIP_DEFLATED) as z:
    z.writestr("manifest.json",manifest)
    z.writestr("color.png",png(192))
    z.writestr("outline.png",png(32,True))
print(root/"ht-voice-teams.zip")
