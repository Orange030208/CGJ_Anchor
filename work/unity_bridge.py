import json,socket,struct,sys
def call(command,params=None):
    with socket.create_connection(('127.0.0.1',6400),timeout=45) as s:
        def read(n):
            b=b''
            while len(b)<n:
                c=s.recv(n-len(b))
                if not c: raise ConnectionError('Unity disconnected')
                b+=c
            return b
        g=b''
        while not g.endswith(b'\n'): g+=read(1)
        p=json.dumps({'type':command,'params':params or {}}).encode()
        s.sendall(struct.pack('>Q',len(p))+p)
        return json.loads(read(struct.unpack('>Q',read(8))[0]))
if __name__=='__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    print(json.dumps(call(sys.argv[1],json.loads(sys.argv[2]) if len(sys.argv)>2 else {}),ensure_ascii=False))
