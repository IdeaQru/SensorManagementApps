const net = require('net');

const HOST = '127.0.0.1';
const PORT = 5000;

// ✅ Kirim ke A4 dan A5 (sesuai permintaan)
const sensors = [
    { address: 'A1', min: 20, max: 35, unit: '°C', label: 'Kelembapan' },
    { address: 'A2', min: 15, max: 45, unit: '°C', label: 'Suhu' },
];

function randomValue(min, max) {
    return (Math.random() * (max - min) + min).toFixed(2);
}

function sendSensorData() {
    const client = new net.Socket();
    let counter = 0;

    client.connect(PORT, HOST, () => {
        console.log('✅ Connected to TCP server at ' + HOST + ':' + PORT);
        console.log('📡 Sending data to A4 and A5...');
        console.log('⚠️  These will be IGNORED if not in database!\n');

        const interval = setInterval(() => {
            console.log(`--- Cycle ${counter} ---`);
            
            sensors.forEach((sensor) => {
                const value = randomValue(sensor.min, sensor.max);
                
                // ✅ Kirim HANYA Address:Value
                const message = `${sensor.address}:${value}\n`;
                client.write(message);
                console.log(`📡 ${sensor.address} = ${value} ${sensor.unit} (${sensor.label})`);
            });

            counter++;
            console.log();
        }, 2000);

        process.on('SIGINT', () => {
            console.log('\n⏸ Stopped by user');
            clearInterval(interval);
            client.destroy();
            process.exit(0);
        });
    });

    client.on('error', (err) => {
        console.error('❌ Connection error:', err.message);
        console.log('\nTroubleshooting:');
        console.log('1. Pastikan aplikasi WPF sudah running');
        console.log('2. Connection tab → Connect TCP');
        console.log('3. Monitoring tab → Select group → Start');
        process.exit(1);
    });

    client.on('close', () => {
        console.log('🔌 TCP connection closed');
    });
}

console.log('='.repeat(60));
console.log('📡 WPF Sensor Monitor - Test A4 & A5');
console.log('='.repeat(60));
console.log('Sending to:');
sensors.forEach(s => {
    console.log(`  📍 ${s.address} → ${s.label} (${s.min}-${s.max}${s.unit})`);
});
console.log('\n💡 Note: Data will only appear if A4 & A5 exist in database');
console.log('='.repeat(60));
console.log();

sendSensorData();
