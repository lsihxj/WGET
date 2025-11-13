import axios from 'axios';

const apiClient = axios.create({
  baseURL: '/api',
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
  },
});

// 响应拦截器
apiClient.interceptors.response.use(
  (response) => response.data,
  (error) => {
    const message = error.response?.data?.message || '请求失败';
    return Promise.reject(new Error(message));
  }
);

export interface ApiResponse<T = any> {
  code: number;
  message: string;
  data?: T;
}

export interface CrawlRule {
  id: string;
  name: string;
  websiteUrl: string;
  inputSelector: string;
  submitSelector?: string;
  resultSelectors: Record<string, string>;
  waitTime: number;
  timeout: number;
  enabled: boolean;
}

export interface CrawlTask {
  task_id: string;
  rule_id?: string;
  status: 'pending' | 'running' | 'completed' | 'failed';
  total_count: number;
  success_count: number;
  failed_count: number;
  created_at: string;
  completed_at?: string;
  results?: CrawlResult[];
}

export interface CrawlResult {
  model_number: string;
  status: 'success' | 'failed';
  data: Record<string, any>;
  error_message?: string;
  duration?: number;
  retry_count?: number;
}

// 规则API
export const ruleApi = {
  getAll: (page = 1, pageSize = 20) =>
    apiClient.get<ApiResponse<CrawlRule[]>>('/rules'),

  getById: (id: string) =>
    apiClient.get<ApiResponse<CrawlRule>>(`/rules/${id}`),
};

// 抓取API
export const crawlApi = {
  submit: (models: string[], useCache = true, retryTimes = 3) =>
    apiClient.post<ApiResponse<{ taskId: string; status: string; totalCount: number }>>('/crawl', {
      models: models,
      useCache: useCache,
      retryTimes: retryTimes,
    }),

  getTask: (taskId: string) =>
    apiClient.get<ApiResponse<any>>(`/tasks/${taskId}`),
};

export default apiClient;
