export const PHONE_REQUIREMENT_MESSAGE =
  'Số điện thoại phải gồm 10 hoặc 11 chữ số và bắt đầu bằng 0 (ví dụ: 0987654321).';

const PHONE_PATTERN = /^0\d{9,10}$/;

export function isValidPhone(phone: string): boolean {
  return PHONE_PATTERN.test(phone);
}

export function getPhoneValidationError(phone: string): string | null {
  if (!phone.trim()) {
    return 'Số điện thoại không được để trống.';
  }

  if (!isValidPhone(phone)) {
    return PHONE_REQUIREMENT_MESSAGE;
  }

  return null;
}