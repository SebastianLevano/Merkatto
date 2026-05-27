export type Role = 1 | 2; // 1 = Administrator, 2 = Collaborator

export interface User {
  id: number;
  email: string;
  fullName: string;
  role: Role;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResult {
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
  user: User;
}
