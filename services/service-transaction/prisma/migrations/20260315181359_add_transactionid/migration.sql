/*
  Warnings:

  - A unique constraint covering the columns `[transactionId]` on the table `transaction_audits` will be added. If there are existing duplicate values, this will fail.
  - A unique constraint covering the columns `[name]` on the table `transaction_types` will be added. If there are existing duplicate values, this will fail.
  - Added the required column `transactionId` to the `transaction_audits` table without a default value. This is not possible if the table is not empty.

*/
-- AlterTable
ALTER TABLE "transaction_audits" ADD COLUMN     "transactionId" TEXT NOT NULL;

-- CreateIndex
CREATE UNIQUE INDEX "transaction_audits_transactionId_key" ON "transaction_audits"("transactionId");

-- CreateIndex
CREATE UNIQUE INDEX "transaction_types_name_key" ON "transaction_types"("name");
