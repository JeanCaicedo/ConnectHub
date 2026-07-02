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

export interface UserProfile {
  id: number;
  username: string;
  bio?: string;
  avatarUrl?: string;
  createdAt: string;
  postsCount: number;
  followersCount: number;
  followingCount: number;
  isFollowedByCurrentUser: boolean;
}

// Respuesta de los endpoints de follow/unfollow
export interface FollowResponse {
  following: boolean;
  followersCount: number;
}

export type NotificationType = 'Like' | 'Comment' | 'Follow';

export interface Notification {
  id: number;
  type: NotificationType;
  fromUserId: number;
  fromUsername: string;
  fromUserAvatarUrl?: string;
  postId?: number;
  isRead: boolean;
  createdAt: string;
}

export interface DailyCount {
  date: string;
  count: number;
}

export interface TopPost {
  id: number;
  content: string;
  likesCount: number;
  commentsCount: number;
}

export interface Engagement {
  postsCount: number;
  likesReceived: number;
  commentsReceived: number;
  followersCount: number;
  engagementRate: number;
}

export interface UserSummary {
  id: number;
  username: string;
  avatarUrl?: string;
  bio?: string;
}

export interface SearchResult {
  posts: Post[];
  users: UserSummary[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
  hasMore: boolean;
}
