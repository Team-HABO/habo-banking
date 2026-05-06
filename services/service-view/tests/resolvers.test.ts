import { describe, it, expect } from 'vitest';
import { resolvers } from '../src/graphql/resolvers';

describe('Audit.timestamp resolver', () => {
  it('returns ISO string for Date input', () => {
    const parent = { timestamp: new Date('2026-04-06T09:22:00Z') };
    const res = resolvers.Audit.timestamp(parent as any);
    expect(res).toBe('2026-04-06T09:22:00.000Z');
  });

  it('parses numeric-string milliseconds input to ISO', () => {
    const ms = Date.parse('2026-04-06T09:22:00Z');
    const parent = { timestamp: String(ms) };
    const res = resolvers.Audit.timestamp(parent as any);
    expect(res).toBe(new Date(ms).toISOString());
  });

  it('returns null for missing or invalid timestamp', () => {
    expect(resolvers.Audit.timestamp({} as any)).toBeNull();
    expect(resolvers.Audit.timestamp({ timestamp: 'not-a-date' } as any)).toBeNull();
  });
});
