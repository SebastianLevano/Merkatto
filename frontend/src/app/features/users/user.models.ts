export type Role = 1 | 2; // 1 = Administrator, 2 = Encargado

export interface UserItem {
  id: number;
  email: string;
  fullName: string;
  role: Role;
  isActive: boolean;
  mustChangePassword: boolean;
  lastLoginAt: string | null;
  businessName: string | null;
}

export interface CreateUserRequest {
  email: string;
  fullName: string;
  role: Role;
  password: string;
  businessName: string | null;
}

export interface UpdateUserRequest {
  fullName: string;
  role: Role;
  isActive: boolean;
  businessName: string | null;
}

export const ROLE_LABEL: Record<Role, string> = {
  1: 'Administrador',
  2: 'Encargado'
};
