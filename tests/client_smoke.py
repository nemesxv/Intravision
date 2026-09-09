"""HTTP/SQL integration checks. Creates and drops an isolated LocalDB database.
Run from any directory with Python 3, .NET/EF CLI and sqlcmd installed.
"""
import os, sys, re, json, time, uuid, html, pathlib, tempfile, shutil, subprocess, urllib.request, urllib.error, urllib.parse, http.cookiejar
from concurrent.futures import ThreadPoolExecutor

ROOT=pathlib.Path(os.environ.get('INTRAVISION_ROOT', pathlib.Path(__file__).resolve().parents[1]))
CONFIGURATION=os.environ.get('INTRAVISION_TEST_CONFIGURATION','Release')
DB='Intravision_ClientTests_'+uuid.uuid4().hex
SERVER=os.environ.get('INTRAVISION_TEST_SERVER',r'(localdb)\MSSQLLocalDB')
CONNECTION=f'Server={SERVER};Database={DB};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True'
BASE='http://127.0.0.1:5192'

def sql(query, database=None):
    result=subprocess.run(['sqlcmd','-S',SERVER,'-d',database or DB,'-E','-b','-h','-1','-W','-Q','SET NOCOUNT ON; '+query],capture_output=True,text=True)
    assert result.returncode==0,result.stdout+result.stderr
    return result.stdout.strip()

class Customer:
    def __init__(self):
        self.jar=http.cookiejar.CookieJar()
        self.opener=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(self.jar))
        self.refresh()
    def request(self,path,fields=None):
        data=None if fields is None else urllib.parse.urlencode(fields).encode()
        try: r=self.opener.open(BASE+path,data=data)
        except urllib.error.HTTPError as e: r=e
        raw=r.read().decode('utf-8',errors='replace')
        try: raw=json.dumps(json.loads(raw),ensure_ascii=False)
        except ValueError: pass
        return r.status,raw
    def refresh(self):
        code,self.text=self.request('/api/vending'); assert code==200,(code,self.text)
        self.state=json.loads(self.text); self.balance=self.state['balance']
    def form(self,action,**fields):
        data={'__RequestVerificationToken':self.state['token'],'version':self.state['version'],**fields}
        if action=='/api/vending/purchase':
            data['drinkVersion']=next(d['rowVersion'] for d in self.state['drinks'] if d['id']==int(fields['drinkId']))
        return {'action':action,'fields':data}
    def post(self,form,**changes):
        fields=dict(form['fields']); fields.update(changes)
        code,text=self.request(form['action'],fields); assert code in (200,409),(code,text[:500])
        self.refresh(); return text
    def insert(self,n): return self.post(self.form('/api/vending/insert',denomination=n))
    def buy(self,id): return self.post(self.form('/api/vending/purchase',drinkId=id))
    def refund(self): return self.post(self.form('/api/vending/return'))

def stock(id): return int(sql(f'SELECT Quantity FROM Drinks WHERE Id={id}'))
def coins(): return sql('SELECT Denomination,Quantity,IsBlocked FROM Coins ORDER BY Denomination')
def fixture(price,quantity=2):
    return int(sql(f"INSERT INTO Drinks(Name,ImagePath,Price,Quantity) OUTPUT inserted.Id VALUES ('Test','/images/drinks/test.png',{price},{quantity})"))
def clear_coins(): sql('UPDATE Coins SET Quantity=0,IsBlocked=0')

server=None
webroot=tempfile.TemporaryDirectory(prefix='IntravisionTests_')
try:
    shutil.copytree(ROOT/'Intravision/wwwroot/app',pathlib.Path(webroot.name)/'app')
    migration=subprocess.run(['dotnet','ef','database','update','--project','Intravision','--configuration',CONFIGURATION,'--connection',CONNECTION],cwd=ROOT,capture_output=True,text=True)
    assert migration.returncode==0,migration.stdout+migration.stderr
    env=dict(os.environ,ASPNETCORE_ENVIRONMENT='Development',ConnectionStrings__DefaultConnection=CONNECTION,Admin__SecretKey='client-tests-only')
    server=subprocess.Popen(['dotnet',str(ROOT/f'Intravision/bin/{CONFIGURATION}/net10.0/Intravision.dll'),'--urls',BASE,'--contentRoot',str(ROOT/'Intravision'),'--webroot',webroot.name],cwd=ROOT,env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
    for _ in range(50):
        try: customer=Customer(); break
        except Exception: time.sleep(.2)
    else: raise AssertionError('Server did not start')
    assert customer.balance==0 and customer.state['drinks']==[]
    assert customer.request('/')[0]==200
    assert customer.request('/admin')[0]==403
    assert customer.request('/api/admin?key=wrong')[0]==403
    assert customer.request('/api/admin?key=client-tests-only&key=client-tests-only')[0]==403
    assert customer.request('/api/vending/insert',{})[0]==400
    initial=coins(); customer.insert(10); assert customer.balance==10 and coins()==initial
    customer.refresh(); assert customer.balance==10
    other=Customer(); assert other.balance==0
    customer.refund(); assert customer.balance==0 and coins()==initial
    print('PASS: empty catalog, anti-forgery, persistent isolated wallets, escrow/refund')

    customer.refresh(); form=customer.form('/api/vending/insert',denomination='1.00')
    sql('UPDATE Coins SET IsBlocked=1 WHERE Denomination=1')
    assert 'недоступен' in customer.post(form) and customer.balance==0
    assert next(c for c in customer.state['coins'] if c['denomination']==1)['isBlocked']
    assert 'недоступен' in customer.post(customer.form('/api/vending/insert',denomination='2.00'),denomination='3')
    clear_coins()
    form=customer.form('/api/vending/insert',denomination='10.00')
    customer.post(form); customer.post(form); assert customer.balance==10
    customer.refund()
    print('PASS: blocked and unsupported denominations; repeated insertion cannot double-credit')

    id=fixture('10'); customer.refresh()
    assert next(d for d in customer.state['drinks'] if d['id']==id)['price']>customer.balance
    assert 'Недостаточно' in customer.buy(id) and stock(id)==2
    customer.insert(10); purchase=customer.form('/api/vending/purchase',drinkId=id)
    customer.post(purchase); assert customer.balance==0 and stock(id)==1
    assert int(sql('SELECT Quantity FROM Coins WHERE Denomination=10'))==1
    customer.post(purchase); assert stock(id)==1
    print('PASS: insufficient funds, exact purchase, inventory credit, duplicate purchase rejection')

    clear_coins(); id=fixture('4'); sql('UPDATE Coins SET Quantity=CASE WHEN Denomination=2 THEN 3 WHEN Denomination=5 THEN 1 ELSE 0 END, IsBlocked=CASE WHEN Denomination=2 THEN 1 ELSE 0 END')
    customer.refresh(); customer.insert(10); text=customer.buy(id)
    assert customer.balance==0 and stock(id)==1
    assert int(sql('SELECT Quantity FROM Coins WHERE Denomination=2'))==0
    assert int(sql('SELECT Quantity FROM Coins WHERE Denomination=5'))==1
    assert customer.state['receipt']['coins']=={'2':3} or customer.state['receipt']['coins']=={'2.00':3}
    print('PASS: bounded change finds 2+2+2 instead of failing at 5+1; blocked coins dispensed')

    clear_coins(); id=fixture('4'); customer.refresh(); customer.insert(10); before=coins()
    assert 'точную сдачу' in customer.buy(id)
    assert stock(id)==2 and coins()==before and customer.balance==10
    customer.refund(); assert coins()==before
    id=fixture('9.50'); customer.refresh(); customer.insert(10)
    assert 'точную сдачу' in customer.buy(id) and stock(id)==2 and customer.balance==10
    customer.refund()
    print('PASS: unavailable and fractional change preserve wallet, drink and machine coins')

    clear_coins(); id=fixture('5'); customer.refresh(); customer.insert(5); old=customer.form('/api/vending/purchase',drinkId=id)
    sql(f'UPDATE Drinks SET Price=4 WHERE Id={id}')
    assert 'Данные напитка изменились' in customer.post(old) and customer.balance==5
    sql(f'UPDATE Drinks SET Quantity=0 WHERE Id={id}')
    customer.refresh(); assert next(d for d in customer.state['drinks'] if d['id']==id)['quantity']==0
    assert 'закончился' in customer.buy(id) and customer.balance==5
    customer.refund()
    print('PASS: changed prices require reconfirmation; sold-out drinks cannot be purchased')

    clear_coins(); id=fixture('10',1)
    a=Customer(); b=Customer(); a.insert(10); b.insert(10)
    with ThreadPoolExecutor(2) as pool:
        results=list(pool.map(lambda c:c.buy(id),[a,b]))
    a.refresh(); b.refresh()
    assert stock(id)==0 and sorted([a.balance,b.balance])==[0,10]
    assert int(sql('SELECT Quantity FROM Coins WHERE Denomination=10'))==1
    (a if a.balance else b).refund()
    print('PASS: concurrent buyers cannot oversell the final drink or lose the losing buyer balance')

    # Two different requests with the same wallet version also cannot consume twice.
    clear_coins(); id=fixture('10',2); a.refresh(); a.insert(10)
    f=a.form('/api/vending/purchase',drinkId=id)
    with ThreadPoolExecutor(2) as pool:
        results=list(pool.map(lambda _:a.request(f['action'],f['fields']),range(2)))
    assert sorted(r[0] for r in results)==[200,409],[r[0] for r in results]
    a.refresh(); assert a.balance==0 and stock(id)==1
    assert int(sql('SELECT Quantity FROM Coins WHERE Denomination=10'))==1
    print('PASS: concurrent duplicate purchase is atomic and debits only once')

    clear_coins(); id=fixture('4',3)
    sql('UPDATE Coins SET Quantity=20 WHERE Denomination IN (1,2)')
    a.refresh(); a.post(a.form('/api/vending/mode'),keepChange='true')
    a.insert(10); a.buy(id); assert a.balance==6 and stock(id)==2
    a.refresh(); assert a.state['keepChange'] and a.state['receipt']['retainedAmount']==6
    a.buy(id); assert a.balance==2 and stock(id)==1
    # Even if administrators remove all machine coins, escrow remains payable.
    clear_coins(); snapshot=coins(); a.refund()
    assert a.balance==0 and coins()==snapshot
    print('PASS: multiple purchases retain exact change, persist mode, and reserve payout against admin edits')

    clear_coins(); id=fixture('4'); a.refresh(); a.insert(10)
    assert 'точную сдачу' in a.buy(id) and a.balance==10
    a.refund(); a.post(a.form('/api/vending/mode'),keepChange='false')
    a.insert(2); a.insert(2); a.buy(id); assert a.balance==0
    print('PASS: multi-purchase rejects unavailable change and can switch back to immediate payout')

    # Exercise import through its protected HTTP endpoint, including whole-file validation.
    admin=Customer()
    status,text=admin.request('/api/admin?key=client-tests-only'); assert status==200
    imp={'action':'/api/admin/drinks/import?key=client-tests-only','fields':{'__RequestVerificationToken':json.loads(text)['token']}}
    png='iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+aD1sAAAAASUVORK5CYII='
    def upload(rows,action=None,with_token=True):
        boundary='optional-import-boundary'
        body=b''
        if with_token:
            for k,v in imp['fields'].items():
                body+=f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode()
        content=rows if isinstance(rows,str) else json.dumps(rows)
        body+=f'--{boundary}\r\nContent-Disposition: form-data; name="importFile"; filename="drinks.json"\r\nContent-Type: application/json\r\n\r\n{content}\r\n--{boundary}--\r\n'.encode()
        req=urllib.request.Request(BASE+(action or imp['action']),data=body,headers={'Content-Type':'multipart/form-data; boundary='+boundary})
        try: r=admin.opener.open(req)
        except urllib.error.HTTPError as e: r=e
        return r.status,json.dumps(json.loads(r.read().decode('utf-8')),ensure_ascii=False)
    row={'name':'Imported drink','price':12.50,'quantity':4,'imageBase64':png}
    assert upload([row],'/api/admin/drinks/import?key=wrong')[0]==403
    assert upload([row],with_token=False)[0]==400
    count=int(sql('SELECT COUNT(*) FROM Drinks'))
    files=lambda: len(list(pathlib.Path(webroot.name).rglob('*.png')))
    initial_files=files()
    assert 'Напиток 2' in upload([row,dict(row,quantity=-1)])[1]
    assert int(sql('SELECT COUNT(*) FROM Drinks'))==count and files()==initial_files
    assert 'Base64' in upload([dict(row,imageBase64='???')])[1]
    assert 'Некорректный JSON' in upload('{invalid')[1]
    assert 'Некорректный JSON' in upload([{'name':'Missing fields'}])[1]
    assert 'от 1 до 50' in upload([row]*51)[1]
    assert 'Импортировано напитков: 2' in upload([row,dict(row,name='Second drink')])[1]
    assert int(sql('SELECT COUNT(*) FROM Drinks'))==count+2 and files()==initial_files+2
    paths=sql("SELECT ImagePath FROM Drinks WHERE Name IN ('Imported drink','Second drink')").splitlines()
    assert all(admin.request(p.strip())[0]==200 for p in paths)
    assert sql("SELECT CAST(Price AS varchar(20)) FROM Drinks WHERE Name='Imported drink'")=='12.50'
    print('PASS: protected import, anti-forgery, atomic validation, required fields, row limits, images, decimal price')

    def admin_forms(edit=''):
        code,text=admin.request('/api/admin?key=client-tests-only'); assert code==200
        state=json.loads(text)
        token={'__RequestVerificationToken':state['token']}
        def form(action,fields): return {'action':'/api/admin/'+action+'?key=client-tests-only','fields':dict(token,**fields)}
        selected=next((d for d in state['drinks'] if edit=='&edit='+str(d['id'])),{'id':0,'name':'','price':0,'quantity':0,'rowVersion':''})
        drink={f'Drink.{k[0].upper()+k[1:]}':v for k,v in selected.items() if k!='imagePath'}
        coin={f'Coins[{i}].{k[0].upper()+k[1:]}':str(v).lower() if isinstance(v,bool) else v for i,c in enumerate(state['coins']) for k,v in c.items()}
        return [form('drinks/save',drink),form('coins/save',coin)]+[form('drinks/delete',{'id':str(d['id']),'rowVersion':d['rowVersion']}) for d in state['drinks']]
    def find_admin(action,edit=''):
        return next(f for f in admin_forms(edit) if action in f['action'])
    def admin_post(f,**changes):
        data=dict(f['fields']); data.update(changes)
        return admin.request(f['action'],data)
    drink_id=int(sql("SELECT Id FROM Drinks WHERE Name='Imported drink'"))
    edit=find_admin('/api/admin/drinks/save','&edit='+str(drink_id))
    assert 'точностью до копеек' in admin_post(edit,**{'Drink.Price':'1.234'})[1]
    assert 'отрицательным' in admin_post(edit,**{'Drink.Quantity':'-1'})[1]
    assert 'Напиток сохранен' in admin_post(edit,**{'Drink.Name':'Edited','Drink.Price':'25.75','Drink.Quantity':'7'})[1]
    assert sql(f'SELECT Quantity FROM Drinks WHERE Id={drink_id}')=='7'
    assert 'Данные уже изменены' in admin_post(edit,**{'Drink.Name':'Stale'})[1]
    current=find_admin('/api/admin/drinks/save','&edit='+str(drink_id))
    assert 'Некорректная версия' in admin_post(current,**{'Drink.RowVersion':'bad'})[1]
    coin=find_admin('/api/admin/coins/save')
    assert 'Настройки монет сохранены' in admin_post(coin,**{'Coins[0].Quantity':'8','Coins[0].IsBlocked':'true','Coins[1].Quantity':'9','Coins[2].IsBlocked':'true'})[1]
    assert sql('SELECT Quantity FROM Coins WHERE Denomination=2')=='9'
    assert sql('SELECT IsBlocked FROM Coins WHERE Denomination=5')=='1'
    before=coins()
    assert 'Данные уже изменены' in admin_post(coin,**{'Coins[3].Quantity':'99'})[1]
    assert coins()==before
    assert 'отрицательным' in admin_post(find_admin('/api/admin/coins/save'),**{'Coins[0].Quantity':'-1','Coins[3].Quantity':'100'})[1]
    assert coins()==before
    fresh=find_admin('/api/admin/coins/save')
    assert 'без повторений' in admin_post(fresh,**{'Coins[1].Denomination':'1'})[1]
    assert coins()==before
    assert 'Настройки монет сохранены' in admin_post(fresh,**{'Coins[0].IsBlocked':'false','Coins[2].IsBlocked':'false'})[1]
    assert sql('SELECT IsBlocked FROM Coins WHERE Denomination=1')=='0'
    delete=next(f for f in admin_forms() if '/api/admin/drinks/delete' in f['action'] and f['fields']['id']==str(drink_id))
    image_path=sql(f'SELECT ImagePath FROM Drinks WHERE Id={drink_id}')
    assert admin_post(delete,rowVersion='bad')[0]==400
    assert 'Напиток удален' in admin_post(delete)[1]
    assert admin.request(image_path)[0]==404
    assert admin_post(delete)[0]==404
    print('PASS: admin validation, edit, stale/malformed versions, coin writes, deletion and image cleanup')

    create_without_image=find_admin('/api/admin/drinks/save')
    assert 'Напиток сохранен' in admin_post(create_without_image,**{
        'Drink.Name':'Drink without image','Drink.Price':'11.50','Drink.Quantity':'3'})[1]
    no_image_id=int(sql("SELECT Id FROM Drinks WHERE Name='Drink without image'"))
    assert sql(f'SELECT COUNT(*) FROM Drinks WHERE Id={no_image_id} AND ImagePath IS NULL')=='1'
    no_image=next(d for d in json.loads(admin.request('/api/admin?key=client-tests-only')[1])['drinks'] if d['id']==no_image_id)
    assert no_image['imagePath'] is None
    no_image_delete=next(f for f in admin_forms() if '/api/admin/drinks/delete' in f['action'] and f['fields']['id']==str(no_image_id))
    assert 'Напиток удален' in admin_post(no_image_delete)[1]
    print('PASS: manual drink creation and deletion without an image')

    def save_image(form, changes, encoded_image):
        import base64
        fields=dict(form['fields']); fields.update(changes)
        boundary='admin-image-boundary'
        body=b''
        for key,value in fields.items():
            if key=='Drink.Image': continue
            body+=f'--{boundary}\r\nContent-Disposition: form-data; name="{key}"\r\n\r\n{value}\r\n'.encode()
        body+=f'--{boundary}\r\nContent-Disposition: form-data; name="Drink.Image"; filename="drink.png"\r\nContent-Type: image/png\r\n\r\n'.encode()
        body+=base64.b64decode(encoded_image)+f'\r\n--{boundary}--\r\n'.encode()
        req=urllib.request.Request(BASE+form['action'],data=body,headers={'Content-Type':'multipart/form-data; boundary='+boundary})
        try: response=admin.opener.open(req)
        except urllib.error.HTTPError as e: response=e
        return json.dumps(json.loads(response.read().decode('utf-8')),ensure_ascii=False)
    create=find_admin('/api/admin/drinks/save')
    assert 'Напиток сохранен' in save_image(create,{'Drink.Name':'Manual drink','Drink.Price':'5.25','Drink.Quantity':'2'},png)
    manual=int(sql("SELECT Id FROM Drinks WHERE Name='Manual drink'"))
    edit=find_admin('/api/admin/drinks/save','&edit='+str(manual))
    previous_path=sql(f'SELECT ImagePath FROM Drinks WHERE Id={manual}')
    assert 'Напиток сохранен' in save_image(edit,{'Drink.Price':'7.50'},png)
    assert admin.request(previous_path)[0]==404
    expected_files=files()
    assert 'Данные уже изменены' in save_image(edit,{},png)
    assert files()==expected_files
    print('PASS: manual creation, image replacement, and stale-write image cleanup')
finally:
    if server: server.terminate(); server.wait(timeout=10)
    # DB is a fresh, fixed-prefix UUID identifier generated by this script.
    assert re.fullmatch(r'Intravision_ClientTests_[0-9a-f]{32}',DB)
    sql(f"IF DB_ID('{DB}') IS NOT NULL BEGIN ALTER DATABASE [{DB}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DB}]; END",'master')
    temp_path=pathlib.Path(webroot.name).resolve()
    assert temp_path.parent==pathlib.Path(tempfile.gettempdir()).resolve() and temp_path.name.startswith('IntravisionTests_')
    webroot.cleanup()
    print('Temporary test database removed.')
