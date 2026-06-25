/** Lấy giá trị hỗ trợ cả camelCase và PascalCase từ ASP.NET */
export function pickField<T>(data: Record<string, unknown>, ...keys: string[]): T | undefined {
  for (const key of keys) {
    if (data[key] !== undefined && data[key] !== null) {
      return data[key] as T;
    }
  }
  return undefined;
}

export function pickString(data: Record<string, unknown>, ...keys: string[]): string {
  const value = pickField<unknown>(data, ...keys);
  return value !== undefined ? String(value) : '';
}

export function pickNumber(data: Record<string, unknown>, ...keys: string[]): number {
  const value = pickField<unknown>(data, ...keys);
  return value !== undefined ? Number(value) : 0;
}

export function pickNullableString(data: Record<string, unknown>, ...keys: string[]): string | null {
  const value = pickField<unknown>(data, ...keys);
  if (value === undefined || value === null || value === '') return null;
  return String(value);
}
