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
  likesCount: number;
  isLikedByCurrentUser: boolean;
  commentsCount: number;
}

export interface Comment {
  id: number;
  content: string;
  createdAt: string;
  userId: number;
  username: string;
  userAvatarUrl?: string;
  parentCommentId?: number;
  replies: Comment[];
}

export interface CreateCommentRequest {
  content: string;
  parentCommentId?: number;
}

export interface CreatePostRequest {
  content: string;
  imageUrl?: string;
}

// Respuesta de los endpoints de like/unlike
export interface LikeResponse {
  liked: boolean;
  likesCount: number;
}
