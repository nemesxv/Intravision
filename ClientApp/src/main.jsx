import React, { useEffect, useRef, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { ArrowUpRight, Plus, Coffee, Coins, Settings2, Check, X, Upload, Search, Pencil, Trash2, RefreshCw, LockKeyhole, ShoppingBag, Wallet, AlertCircle } from 'lucide-react';
import './style.css';

const money = value => new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 2 }).format(value) + ' ₽';
const admin = location.pathname.toLowerCase().replace(/\/+$/, '') === '/admin';
const key = new URLSearchParams(location.search).get('key') || '';
const endpoint = path => `/api/${admin ? 'admin' : 'vending'}${path}${admin ? '?key=' + encodeURIComponent(key) : ''}`;
async function request(path = '', fields, token) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 15000);
  try {
    const response = await fetch(endpoint(path), { method: fields === undefined ? 'GET' : 'POST', credentials: 'same-origin', cache: 'no-store',
      headers: token ? { 'X-CSRF-TOKEN': token } : {}, body: fields === undefined ? undefined : fields instanceof FormData ? fields : new URLSearchParams(fields), signal: controller.signal });
    const body = await response.json().catch(() => ({}));
    if (!response.ok) throw new Error(body.error || (body.errors && Object.values(body.errors).flat().join(' ')) || (response.status === 403 ? 'Доступ запрещён. Проверьте секретный ключ в адресе страницы.' : response.status === 400 ? 'Не удалось проверить запрос. Обновите страницу и повторите попытку.' : 'Не удалось выполнить запрос. Повторите попытку.'));
    return body;
  } catch (error) {
    if (error.name === 'AbortError') throw new Error('Сервер не ответил вовремя. Обновите данные, чтобы проверить результат операции.');
    throw error;
  } finally { clearTimeout(timeout); }
}

function App() {
  const [data, setData] = useState(null), [error, setError] = useState(''), [notice, setNotice] = useState(''), [busy, setBusy] = useState(false);
  const lock = useRef(false);
  async function refresh() { const next = await request(); setData(next); return next; }
  async function load() {
    if (lock.current) return;
    lock.current = true; setBusy(true); setError('');
    try { await refresh(); } catch (e) { setError(e.message); } finally { lock.current = false; setBusy(false); }
  }
  useEffect(() => { load(); }, []);
  async function mutate(path, fields) {
    if (lock.current) return false;
    lock.current = true; setBusy(true); setError(''); setNotice('');
    let success = false;
    try {
      const result = await request(path, fields, data.token);
      success = true; setNotice(result.message || '');
    } catch (e) { setError(e.message); }
    // Read back the authoritative state even after a conflict or a lost response.
    try { await refresh(); } catch (e) { setError(e.message); }
    lock.current = false; setBusy(false); return success;
  }
  return <><header className="topbar"><a className="brand" href="/" aria-label="Intravision — главная"><span className="brand-icon"><Coffee size={22}/></span>intravision<span className="brand-dot">.</span></a><span className="header-caption">{admin ? 'ПАНЕЛЬ УПРАВЛЕНИЯ' : 'ХОРОШИЙ ДЕНЬ НАЧИНАЕТСЯ С НАПИТКА'}</span>{admin ? <a className="nav-link" href="/">К автомату <ArrowUpRight size={17}/></a> : <span className="online"><i/> {data && !error ? "Автомат онлайн" : "Ваш автомат напитков"}</span>}</header>
    <main>{error && <div className="alert error" role="alert"><AlertCircle size={20}/><span>{error}</span><button className="text-button" onClick={load} disabled={busy}>Обновить</button></div>}{notice && <div className="alert success" role="status"><Check size={20}/>{notice}</div>}
    {!data ? <div className="loading"><Coffee size={40}/><h1>{busy ? 'Готовим ваш автомат…' : 'Не удалось загрузить автомат'}</h1><p>{busy ? 'Получаем ассортимент и настройки.' : 'Проверьте подключение и попробуйте ещё раз.'}</p>{!busy && <button className="primary" onClick={load}><RefreshCw size={17}/> Повторить</button>}</div> : admin ? <Admin data={data} busy={busy} mutate={mutate} error={error}/> : <Customer data={data} busy={busy} mutate={mutate}/>}</main>
    <footer><span>intravision <span className="muted">/ автомат по продаже напитков</span></span><span>Небольшой перерыв. Большое удовольствие.</span></footer></>;
}

function DrinkImage({ drink }) {
  const [failed, setFailed] = useState(false);
  useEffect(() => setFailed(false), [drink.imagePath]);
  return failed || !drink.imagePath ? <Coffee className="drink-fallback" size={64} strokeWidth={1.2}/> : <img src={drink.imagePath} alt={drink.name} onError={() => setFailed(true)}/>;
}

function Customer({ data, busy, mutate }) {
  const available = data.drinks.filter(d => d.quantity > 0).length;
  return <><div className="page-heading"><div><span className="eyebrow">ВАШ МАЛЕНЬКИЙ ПЕРЕРЫВ</span><h1>Выберите настроение<span>.</span></h1><p>Внесите монеты, выберите напиток — и наслаждайтесь.</p></div><span className="heading-stamp"><Coffee size={20}/> Всегда рядом</span></div>
  <div className="shop-layout"><section className="catalog"><div className="section-title"><h2>Наши напитки <span className="count">{data.drinks.length}</span></h2><span className="muted">{available} в наличии</span></div>
    {!data.drinks.length ? <div className="empty"><ShoppingBag size={42} strokeWidth={1.3}/><h3>Скоро здесь будут напитки</h3><p>Ассортимент пока пуст. Администратор может добавить напитки или импортировать каталог.</p></div> : <div className="drink-grid">{data.drinks.map((drink, index) => {
      const sold = drink.quantity === 0, affordable = drink.price <= data.balance;
      return <button key={drink.id} className={`drink-card tone-${index % 4} ${sold ? 'sold-out' : ''}`} disabled={busy || sold || !affordable} onClick={() => mutate('/purchase', { drinkId: drink.id, version: data.version, drinkVersion: drink.rowVersion })} aria-label={`Купить ${drink.name}, ${money(drink.price)}${sold ? ', закончился' : !affordable ? ', недостаточно средств' : ''}`}>
        <div className="drink-art"><span className={`stock ${sold ? 'unavailable' : ''}`}>{sold ? 'Закончился' : `${drink.quantity} шт.`}</span><DrinkImage drink={drink}/><span className="art-circle"/></div>
        <div className="drink-info"><h3>{drink.name}</h3><div className="drink-bottom"><strong>{money(drink.price)}</strong><span className="buy-icon"><Plus size={21}/></span></div><p>{sold ? 'Скоро пополним' : affordable ? 'Нажмите, чтобы купить' : `Внесите ещё ${money(drink.price - data.balance)}`}</p></div>
      </button>;
    })}</div>}
    <div className="how-it-works"><span><b>01</b> Внесите монеты</span><span><b>02</b> Выберите напиток</span><span><b>03</b> Заберите сдачу</span></div>
  </section><aside className="payment"><div className="payment-heading"><span><Wallet size={19}/> Ваш баланс</span><span className="live-dot"/></div><div className="balance" aria-live="polite">{money(data.balance)}</div><p className="payment-caption">Внесите сумму кнопками ниже</p><div className="coin-buttons">{data.coins.map(c => <button key={c.denomination} disabled={busy || c.isBlocked} className={c.isBlocked ? 'blocked' : ''} onClick={() => mutate('/insert', { denomination: c.denomination, version: data.version })} aria-label={`Внести ${money(c.denomination)}${c.isBlocked ? ', приём заблокирован' : ''}`}><span>{c.denomination}</span><small>{c.isBlocked ? 'закрыт' : 'рублей'}</small></button>)}</div>
    <div className="payment-divider"/><div className="mode-title"><ShoppingBag size={18}/> Режим покупки</div><div className="mode-options">{[[false, 'Один напиток', 'Сдача сразу после покупки'], [true, 'Несколько напитков', 'Сдача, когда закончите']].map(([mode, title, text]) => <label key={title} className={data.keepChange === mode ? 'selected' : ''}><input type="radio" name="mode" checked={data.keepChange === mode} disabled={busy} onChange={() => mutate('/mode', { keepChange: mode, version: data.version })}/><span><b>{title}</b><small>{text}</small></span></label>)}</div>
    <button className="return-button" disabled={busy || data.balance <= 0} onClick={() => mutate('/return', { version: data.version })}><Coins size={19}/> Забрать {data.balance > 0 ? money(data.balance) : 'сдачу'} <ArrowUpRight size={19}/></button><p className="payment-note">{busy ? 'Выполняем операцию…' : 'Передумали? Внесённые монеты можно вернуть.'}</p>
  </aside></div>{data.receipt && <section className="receipt" aria-live="polite"><span className="receipt-icon"><Check size={25}/></span><div><h3>{data.receipt.message}</h3><p>Выдано: {money(data.receipt.returnedAmount)}{data.receipt.retainedAmount > 0 ? ` · Осталось на балансе: ${money(data.receipt.retainedAmount)}` : ''}</p>{Object.keys(data.receipt.coins).length > 0 && <div className="change-coins">{Object.entries(data.receipt.coins).map(([coin, count]) => <span key={coin}>{money(Number(coin))} × {count}</span>)}</div>}</div></section>}</>;
}

function Admin({ data, busy, mutate, error }) {
  const [search, setSearch] = useState(''), [editor, setEditor] = useState(null), [coins, setCoins] = useState(data.coins), [dirty, setDirty] = useState(false), [importFile, setImportFile] = useState(null), [deleteDrink, setDeleteDrink] = useState(null);
  const importRef = useRef(null);
  useEffect(() => { if (!dirty) setCoins(data.coins); }, [data.coins, dirty]);
  const filtered = data.drinks.filter(d => d.name.toLocaleLowerCase().includes(search.toLocaleLowerCase()));
  async function saveCoins(e) {
    e.preventDefault(); const fields = {};
    coins.forEach((c, i) => Object.entries(c).forEach(([k, v]) => { fields[`Coins[${i}].${k}`] = v; }));
    if (await mutate('/coins/save', fields)) setDirty(false);
  }
  function changeCoin(i, field, value) { setDirty(true); setCoins(coins.map((c, n) => n === i ? { ...c, [field]: value } : c)); }
  return <><div className="page-heading"><div><span className="eyebrow">ВСЁ ПОД КОНТРОЛЕМ</span><h1>Управление автоматом<span>.</span></h1><p>Ассортимент, запасы и настройки приёма монет.</p></div><span className="access-badge"><LockKeyhole size={16}/> Доступ по ключу</span></div>
  <div className="stats"><div><span>Напитков в каталоге</span><strong>{data.drinks.length}<Coffee size={23}/></strong></div><div><span>Всего напитков в запасе</span><strong>{data.drinks.reduce((n, d) => n + d.quantity, 0)}<ShoppingBag size={23}/></strong></div><div><span>Монет в автомате</span><strong>{money(data.coins.reduce((n, c) => n + c.quantity * c.denomination, 0))}<Coins size={23}/></strong></div></div>
  <div className="admin-layout"><section className="panel assortment"><div className="panel-heading"><div><h2>Ассортимент</h2><p>Напитки, которые видят покупатели</p></div><button className="primary" disabled={busy} onClick={() => setEditor({ id: 0, name: '', price: '', quantity: 0 })}><Plus size={17}/> Добавить напиток</button></div><label className="search"><Search size={18}/><input aria-label="Поиск напитка" placeholder="Найти напиток…" value={search} onChange={e => setSearch(e.target.value)}/></label>
  <div className="table-wrap"><table><thead><tr><th>Напиток</th><th>Цена</th><th>Остаток</th><th><span className="sr-only">Действия</span></th></tr></thead><tbody>{filtered.map(d => <tr key={d.id}><td><div className="table-drink"><span className="thumbnail"><DrinkImage drink={d}/></span><b>{d.name}</b></div></td><td className="nowrap">{money(d.price)}</td><td><span className={`stock-pill ${d.quantity === 0 ? 'zero' : ''}`}>{d.quantity} шт.</span></td><td><div className="row-actions"><button className="icon-button" aria-label={`Изменить ${d.name}`} disabled={busy} onClick={() => setEditor(d)}><Pencil size={17}/></button><button className="icon-button danger" aria-label={`Удалить ${d.name}`} disabled={busy} onClick={() => setDeleteDrink(d)}><Trash2 size={17}/></button></div></td></tr>)}</tbody></table></div>{!filtered.length && <div className="empty compact"><Coffee size={36}/><h3>{search ? 'Ничего не найдено' : 'Добавьте первый напиток'}</h3><p>{search ? 'Попробуйте другое название.' : 'Создайте напиток или загрузите каталог из JSON.'}</p></div>}<div className="table-footer">Показано {filtered.length} из {data.drinks.length}</div>
  <form className="import-box" onSubmit={async e => { e.preventDefault(); const form = new FormData(); form.append('importFile', importFile); if (await mutate('/drinks/import', form)) { setImportFile(null); importRef.current.value = ''; } }}><span className="import-icon"><Upload size={23}/></span><div><h3>Импорт каталога</h3><p>JSON · до 50 напитков · до 20 МБ</p><input ref={importRef} aria-label="JSON-файл для импорта" type="file" accept=".json,application/json" required disabled={busy} onChange={e => setImportFile(e.target.files[0] || null)}/></div><button className="secondary" disabled={busy || !importFile}>Импортировать</button></form>
  </section><section className="panel coin-panel"><div className="panel-heading"><div><h2><Coins size={22}/> Монеты</h2><p>Запас для выдачи сдачи</p></div></div><form onSubmit={saveCoins}><div className="coin-labels"><span>Номинал</span><span>Количество</span><span>Приём</span></div>{coins.map((c, i) => <div className="coin-row" key={c.denomination}><span className="denomination">{c.denomination}<small>₽</small></span><input type="number" min="0" max="2147483647" step="1" required aria-label={`Количество монет ${c.denomination} рублей`} value={c.quantity} disabled={busy} onChange={e => changeCoin(i, 'quantity', e.target.value)}/><label className="switch"><input type="checkbox" checked={!c.isBlocked} disabled={busy} aria-label={`Принимать монеты ${c.denomination} рублей`} onChange={e => changeCoin(i, 'isBlocked', !e.target.checked)}/><span/></label></div>)}<div className="coin-help"><Settings2 size={18}/><p>Выключите приём, чтобы заблокировать номинал. Эти монеты всё равно можно выдавать на сдачу.</p></div><button className="primary save-coins" disabled={busy || !dirty}><Check size={18}/> {busy ? 'Сохраняем…' : 'Сохранить монеты'}</button>{dirty && <button type="button" className="text-button reset-coins" disabled={busy} onClick={() => { setCoins(data.coins); setDirty(false); }}>Сбросить изменения</button>}</form></section></div>
  {editor && <DrinkEditor error={error} drink={editor} busy={busy} onClose={() => setEditor(null)} onSave={async form => { if (await mutate('/drinks/save', form)) setEditor(null); }}/>} {deleteDrink && <Modal error={error} title="Удалить напиток?" busy={busy} onClose={() => setDeleteDrink(null)}><p>«{deleteDrink.name}» будет удалён из ассортимента вместе с изображением.</p><div className="modal-actions"><button className="secondary" disabled={busy} onClick={() => setDeleteDrink(null)}>Отмена</button><button className="primary destructive" disabled={busy} onClick={async () => { if (await mutate('/drinks/delete', { id: deleteDrink.id, rowVersion: deleteDrink.rowVersion })) setDeleteDrink(null); }}>Удалить</button></div></Modal>}</>;
}

function Modal({ title, busy, onClose, children, error }) {
  const ref = useRef(null), closeRef = useRef(onClose), busyRef = useRef(busy);
  closeRef.current = onClose; busyRef.current = busy;
  useEffect(() => { const el = ref.current; const previous = document.activeElement; el.showModal(); return () => { el.close(); previous?.focus(); }; }, []);
  return <dialog ref={ref} className="modal" onCancel={e => { e.preventDefault(); if (!busyRef.current) closeRef.current(); }}><div className="modal-heading"><h2>{title}</h2><button className="icon-button" aria-label="Закрыть" disabled={busy} onClick={onClose}><X size={21}/></button></div>{error && <div className="alert error" role="alert">{error}</div>}{children}</dialog>;
}

function DrinkEditor({ drink, busy, onClose, onSave, error }) {
  const [preview, setPreview] = useState(drink.imagePath || ''), [file, setFile] = useState(null);
  useEffect(() => { if (!file) return; const url = URL.createObjectURL(file); setPreview(url); return () => URL.revokeObjectURL(url); }, [file]);
  return <Modal error={error} title={drink.id ? 'Редактировать напиток' : 'Новый напиток'} busy={busy} onClose={onClose}><form onSubmit={e => { e.preventDefault(); onSave(new FormData(e.currentTarget)); }}><fieldset disabled={busy}><input type="hidden" name="Drink.Id" value={drink.id}/><input type="hidden" name="Drink.RowVersion" value={drink.rowVersion || ''}/><label className="field">Название<input name="Drink.Name" defaultValue={drink.name} maxLength="100" required autoFocus placeholder="Например, капучино"/></label><div className="field-pair"><label className="field">Цена, ₽<input type="number" name="Drink.Price" defaultValue={drink.price} min="0.01" max="99999999.99" step="0.01" required/></label><label className="field">Количество<input type="number" name="Drink.Quantity" defaultValue={drink.quantity} min="0" max="2147483647" step="1" required/></label></div><label className="field">Изображение <small>(необязательно)</small><div className="upload-preview">{preview ? <img src={preview} alt="Предпросмотр напитка"/> : <Coffee size={38}/>}<span>PNG, JPEG или WebP<br/><small>До 2 МБ</small></span></div><input name="Drink.Image" type="file" accept="image/png,image/jpeg,image/webp" onChange={e => setFile(e.target.files[0] || null)}/></label><div className="modal-actions"><button type="button" className="secondary" onClick={onClose}>Отмена</button><button className="primary">{busy ? 'Сохраняем…' : 'Сохранить напиток'}</button></div></fieldset></form></Modal>;
}

createRoot(document.getElementById('root')).render(<App/>);
