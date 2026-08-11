async function run() {
  try {
    const loginRes = await fetch('http://localhost:5254/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'admin@gmail.com', password: '123' })
    });
    const { token } = await loginRes.json();

    const quoteRes = await fetch('http://localhost:5254/api/scrape-orders/quote', {
      method: 'POST',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ mentionsPackage: 'FULL_UNLIMITED' })
    });
    const data = await quoteRes.json();
    console.log('STATUS:', quoteRes.status);
    console.log('SUCCESS:', data);
  } catch (err) {
    console.log('ERROR:', err.message);
  }
}
run();
