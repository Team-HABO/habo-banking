# Prisma

## Database Schema

The application uses Prisma ORM with PostgreSQL for managing transaction data. The schema includes:

- **Balance**: User account balances with unique owner and account identifiers
- **BalanceDetail**: Balance records with timestamps
- **DeletedBalance**: Deleted balances
- **TransactionAudit**: Complete bank transaction records
- **TransactionType**: Transaction type definitions (deposit, withdrawal, transfer, etc.)

## Commands

**Database Setup & Migrations:**

```bash
# Create and apply a new migration
npx prisma migrate dev --name migration_name

# Apply pending migrations in production
npx prisma migrate deploy

# Check migration status
npx prisma migrate status
```

**Client Generation:**

```bash
# Generate Prisma Client after schema changes
npx prisma generate
```

**Database Seeding:**

```bash
# Seed database with data defined in prisma/seed.ts
npx prisma db seed
```

**Database Management:**

```bash
# Open Prisma Studio (visual database editor)
npx prisma studio

# Validate schema file
npx prisma validate

# Format schema file
npx prisma format
```

**Introspection:**

```bash
# Pull schema from existing database
npx prisma db pull
```

## Environment Variables

Ensure `DATABASE_URL` is set in your `.env` file:

```
DATABASE_URL="postgresql://USER:PASSWORD@postgres:5432/DATABASE"
```
