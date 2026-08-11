async function run() {
  try {
    const loginRes = await fetch('http://localhost:5254/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: 'admin@gmail.com', password: '123' })
    });
    const { token } = await loginRes.json();

    const payRes = await fetch('http://localhost:5254/api/scrape-orders/1/pay', {
      method: 'POST',
      headers: { 
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      }
    });
    const data = await payRes.json();
    console.log('STATUS:', payRes.status);
    console.log('SUCCESS:', data);
  } catch (err) {
    console.log('ERROR:', err.message);
  }
}
run();
