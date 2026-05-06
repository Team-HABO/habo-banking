import mongoose, { Model } from "mongoose";

const AuditSchema = new mongoose.Schema({
  auditId: String,
  amount: String,
  type: String,
  timestamp: Date,
  sender: String,
  receiver: String
});
const AccountSchema = new mongoose.Schema({
  accountGuid: { type: String, required: true },
  type: String,
  name: String,
  timestamp: Date,
  isFrozen: Boolean,
  balance: {
    amount: mongoose.Schema.Types.Decimal128 
  },
  audits: [AuditSchema]
});

const UserSchema = new mongoose.Schema({
  _id: String, 
  accounts: [AccountSchema]
});

export const User: Model<any> = mongoose.model('User', UserSchema);