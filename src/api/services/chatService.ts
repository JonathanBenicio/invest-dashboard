import { api } from '@/api/client'

export interface ChatResponse {
  message: string
}

export const chatService = {
  sendMessage: async (userId: string, message: string): Promise<ChatResponse> => {
    return api.post<ChatResponse>('/chat', { userId, message })
  },
}
