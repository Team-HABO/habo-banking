import { prisma } from "../../prisma/prisma";

export function isOlderEvent(savedDate: Date, payloadDate: Date) {
	return savedDate >= payloadDate;
}

export async function isAlreadyProcessed(messageId: string) {
	const audit = await prisma.transactionAudit.findUnique({
		where: { transactionId: messageId }
	});

	return audit ? true : false;
}
