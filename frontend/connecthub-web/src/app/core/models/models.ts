export interface RegisterRequest {
  username: string;
  email: string;
  password: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthResponse {
  token: string;
  userId: number;
  username: string;
  email: string;
}

export interface Post {
  id: number;
  content: string;
  imageUrl?: string;
  createdAt: string;
  userId: number;
  username: string;
  userAvatarUrl?: string;
}

export interface CreatePostRequest {
  content: string;
  imageUrl?: string;
}
