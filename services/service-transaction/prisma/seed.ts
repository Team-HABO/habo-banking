import "dotenv/config";
import { PrismaPg } from "@prisma/adapter-pg";
import { PrismaClient } from "../generated/prisma/client";

const connectionString = `${process.env.DATABASE_URL}`;
const adapter = new PrismaPg({ connectionString });
const prisma = new PrismaClient({ adapter });

async function main() {
	await prisma.transactionType.upsert({
		where: { name: "DEPOSIT" },
		update: {},
		create: {
			name: "DEPOSIT"
		}
	});
	await prisma.transactionType.upsert({
		where: { name: "WITHDRAWAL" },
		update: {},
		create: {
			name: "WITHDRAWAL"
		}
	});
	await prisma.transactionType.upsert({
		where: { name: "P2P_SEND" },
		update: {},
		create: {
			name: "P2P_SEND"
		}
	});
	await prisma.transactionType.upsert({
		where: { name: "P2P_RECEIVE" },
		update: {},
		create: {
			name: "P2P_RECEIVE"
		}
	});
	await prisma.transactionType.upsert({
		where: { name: "PAYMENT" },
		update: {},
		create: {
			name: "PAYMENT"
		}
	});
}
main()
	.then(async () => {
		await prisma.$disconnect();
	})
	.catch(async (e) => {
		console.error(e);
		await prisma.$disconnect();
		process.exit(1);
	});
