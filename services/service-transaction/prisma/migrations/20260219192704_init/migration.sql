-- CreateTable
CREATE TABLE "balances" (
    "id" SERIAL NOT NULL,
    "ownerId" TEXT NOT NULL,
    "accountGuid" TEXT NOT NULL,

    CONSTRAINT "balances_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "balance_details" (
    "id" SERIAL NOT NULL,
    "balanceId" INTEGER NOT NULL,
    "amount" TEXT NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "balance_details_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "deleted_balances" (
    "id" SERIAL NOT NULL,
    "balanceId" INTEGER NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "deleted_balances_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "transaction_audits" (
    "id" SERIAL NOT NULL,
    "senderBalanceId" INTEGER NOT NULL,
    "receiverBalanceId" INTEGER NOT NULL,
    "amount" TEXT NOT NULL,
    "typeId" INTEGER NOT NULL,
    "createdAt" TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "transaction_audits_pkey" PRIMARY KEY ("id")
);

-- CreateTable
CREATE TABLE "transaction_types" (
    "id" SERIAL NOT NULL,
    "name" TEXT NOT NULL,

    CONSTRAINT "transaction_types_pkey" PRIMARY KEY ("id")
);

-- CreateIndex
CREATE UNIQUE INDEX "balances_ownerId_key" ON "balances"("ownerId");

-- CreateIndex
CREATE UNIQUE INDEX "balances_accountGuid_key" ON "balances"("accountGuid");

-- AddForeignKey
ALTER TABLE "balance_details" ADD CONSTRAINT "balance_details_balanceId_fkey" FOREIGN KEY ("balanceId") REFERENCES "balances"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "deleted_balances" ADD CONSTRAINT "deleted_balances_balanceId_fkey" FOREIGN KEY ("balanceId") REFERENCES "balances"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction_audits" ADD CONSTRAINT "transaction_audits_senderBalanceId_fkey" FOREIGN KEY ("senderBalanceId") REFERENCES "balances"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction_audits" ADD CONSTRAINT "transaction_audits_receiverBalanceId_fkey" FOREIGN KEY ("receiverBalanceId") REFERENCES "balances"("id") ON DELETE RESTRICT ON UPDATE CASCADE;

-- AddForeignKey
ALTER TABLE "transaction_audits" ADD CONSTRAINT "transaction_audits_typeId_fkey" FOREIGN KEY ("typeId") REFERENCES "transaction_types"("id") ON DELETE RESTRICT ON UPDATE CASCADE;
