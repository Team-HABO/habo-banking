/*
  Warnings:

  - A unique constraint covering the columns `[balanceId]` on the table `deleted_balances` will be added. If there are existing duplicate values, this will fail.

*/
-- CreateIndex
CREATE UNIQUE INDEX "deleted_balances_balanceId_key" ON "deleted_balances"("balanceId");
