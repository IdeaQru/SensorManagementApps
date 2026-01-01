const { SerialPort } = require('serialport');  // ✅ Destructure import

const PORT = 'COM4';
const BAUD_RATE = 9600;

const sensors = [
    { address: 'A1', min: 20, max: 35, unit: '°C', label: 'Kelembapan' },
    { address: 'A2', min: 15, max: 45, unit: '°C', label: 'Suhu' },
];

function randomValue(min, max) {
    return (Math.random() * (max - min) + min).toFixed(2);
}

async function sendSerialData() {
    try {
        const port = new SerialPort({ 
            path: PORT, 
            baudRate: BAUD_RATE 
        });

        port.on('open', () => {
            console.log('✅ Serial port opened:', PORT);
            console.log('📡 Sending sensor data... (Press Ctrl+C to stop)\n');

            let counter = 0;

            const interval = setInterval(() => {
                console.log(`--- Cycle ${counter} ---`);

                sensors.forEach((sensor) => {
                    const value = randomValue(sensor.min, sensor.max);
                    const message = `${sensor.address}:${value}\n`;

                    port.write(message, (err) => {
                        if (err) {
                            console.error('❌ Write error:', err.message);
                        } else {
                            console.log(`📡 ${sensor.address} = ${value} ${sensor.unit} (${sensor.label})`);
                        }
                    });
                });

                counter++;
                console.log();
            }, 2000);

            process.on('SIGINT', () => {
                console.log('\n⏸ Stopped by user');
                clearInterval(interval);
                port.close();
                process.exit(0);
            });
        });

        port.on('error', (err) => {
            console.error('❌ Serial port error:', err.message);
            console.log('\nTroubleshooting:');
            console.log('1. Pastikan COM11 tersedia (Device Manager)');
            console.log('2. Install com0com untuk virtual port pair');
            console.log('3. Di WPF, connect ke COM10 (pair dari COM11)');
            process.exit(1);
        });

    } catch (err) {
        console.error('❌ Error:', err.message);
        process.exit(1);
    }
}

console.log('='.repeat(60));
console.log('📡 WPF Sensor Monitor - Serial Test');
console.log('='.repeat(60));
console.log('Port:', PORT);
console.log('Baud Rate:', BAUD_RATE);
console.log('\nSending to:');
sensors.forEach(s => {
    console.log(`  📍 ${s.address} → ${s.label} (${s.min}-${s.max}${s.unit})`);
});
console.log('\n💡 Setup:');
console.log('  1. Install com0com (virtual serial port)');
console.log('  2. Create pair: COM10 ↔ COM11');
console.log('  3. WPF connect to COM10');
console.log('  4. This script sends to COM11');
console.log('='.repeat(60));
console.log();

sendSerialData();
