export function pickField<T>(data: Record<string, unknown>, ...keys: string[]): T | undefined {
  for (const key of keys) {
    if (key in data && data[key] !== undefined && data[key] !== null) {
      return data[key] as T;
    }
  }
  return undefined;
}

export function pickString(data: Record<string, unknown>, ...keys: string[]): string {
  const value = pickField<string>(data, ...keys);
  return value ?? '';
}

export function pickNullableString(data: Record<string, unknown>, ...keys: string[]): string | null {
  const value = pickField<string>(data, ...keys);
  return value ?? null;
}

export function pickNumber(data: Record<string, unknown>, ...keys: string[]): number {
  const value = pickField<number | string>(data, ...keys);
  if (value === undefined || value === null) return 0;
  const parsed = typeof value === 'number' ? value : Number(value);
  return Number.isFinite(parsed) ? parsed : 0;
}
