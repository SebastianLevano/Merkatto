export type Role = 1 | 2; // 1 = Administrator, 2 = Collaborator

export interface UserItem {
  id: number;
  email: string;
  fullName: string;
  role: Role;
  isActive: boolean;
  mustChangePassword: boolean;
  lastLoginAt: string | null;
}

export interface CreateUserRequest {
  email: string;
  fullName: string;
  role: Role;
  password: string;
}

export interface UpdateUserRequest {
  fullName: string;
  role: Role;
  isActive: boolean;
}

export const ROLE_LABEL: Record<Role, string> = {
  1: 'Administrador',
  2: 'Colaborador'
};
