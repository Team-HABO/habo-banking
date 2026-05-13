import 'dotenv/config';
import mongoose from 'mongoose';

const connectionString = process.env.MONGODB_CONNECTION_STRING!;
async function seed() {
  try {
    await mongoose.connect(connectionString);
    const db = mongoose.connection.db!;

    await db.collection('users').insertOne({
      _id: 'Test' as any,
      accounts: [
        {
          accountGuid: 'my-guid',
          type: 'Savings',
          name: 'my savings',
          timestamp: '2026-04-06T09:21:00Z',
          isFrozen: false,
          balance: {
            amount: mongoose.Types.Decimal128.fromString('650.00'),
          },
          audits: [
            { auditId: 'GUID',  amount: '200.00', type: 'Deposit',  timestamp: '2026-04-06T09:22:00Z', sender: null, receiver: null },
            { auditId: 'GUID2', amount: '500.00', type: 'Deposit',  timestamp: '2026-04-06T09:22:00Z', sender: null, receiver: null },
            { auditId: 'GUID3', amount: '50.00',  type: 'Withdraw', timestamp: '2026-04-06T09:22:00Z', sender: null, receiver: null },
          ],
        },
      ],
    });

    console.log('Seeded successfully');
  } catch (error) {
    console.error('Seed failed:', error);
    process.exit(1);
  } finally {
    await mongoose.disconnect();
  }
}

seed();