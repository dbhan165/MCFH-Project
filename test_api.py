import urllib.request
import json

req1 = urllib.request.Request('http://localhost:5254/api/auth/login', data=json.dumps({"email":"admin@gmail.com","password":"123"}).encode(), headers={'Content-Type': 'application/json'})
resp1 = urllib.request.urlopen(req1)
token = json.loads(resp1.read())['token']

req2 = urllib.request.Request('http://localhost:5254/api/scrape-orders/quote', data=json.dumps({"mentionsPackage":"FULL_UNLIMITED"}).encode(), headers={'Content-Type': 'application/json', 'Authorization': f'Bearer {token}'})
try:
    resp2 = urllib.request.urlopen(req2)
    print("SUCCESS:")
    print(resp2.read().decode())
except Exception as e:
    print("ERROR:", e)
    if hasattr(e, 'read'):
        print(e.read().decode())
