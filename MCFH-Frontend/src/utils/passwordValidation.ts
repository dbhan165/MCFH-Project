export const PASSWORD_REQUIREMENT_MESSAGE =
  'Mật khẩu phải có tối thiểu 8 ký tự, bao gồm chữ in hoa, chữ cái, số và ký tự @.';

const PASSWORD_PATTERN = /^(?=.*[A-Z])(?=.*[a-zA-Z])(?=.*\d)(?=.*@).{8,}$/;

export function isValidPassword(password: string): boolean {
  return PASSWORD_PATTERN.test(password);
}

export function getPasswordValidationError(password: string): string | null {
  if (!password.trim()) {
    return 'Mật khẩu không được để trống.';
  }

  if (!isValidPassword(password)) {
    return PASSWORD_REQUIREMENT_MESSAGE;
  }

  return null;
}
