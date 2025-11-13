import React, { useState, useEffect } from 'react';
import {
  Card,
  Form,
  Select,
  Input,
  Button,
  Table,
  Space,
  message,
  Tag,
  Progress,
} from 'antd';
import { SearchOutlined, ClearOutlined, ReloadOutlined } from '@ant-design/icons';
import { crawlApi, CrawlTask } from '../services/api';

const { TextArea } = Input;
const { Option } = Select;

const CrawlQuery: React.FC = () => {
  const [form] = Form.useForm();
  const [loading, setLoading] = useState(false);
  const [taskLoading, setTaskLoading] = useState(false);
  const [queryResults, setQueryResults] = useState<CrawlTask[]>([]); // 改为数组存储多个结果
  const [polling, setPolling] = useState(false);
  const [currentPollingTaskId, setCurrentPollingTaskId] = useState<string>(''); // 当前轮询的任务ID
  const [startTime, setStartTime] = useState<number>(0);
  const [elapsedTime, setElapsedTime] = useState<number>(0);
  const [totalDuration, setTotalDuration] = useState<number>(0);
  const [queryHistory, setQueryHistory] = useState<CrawlTask[]>([]);

  // 从 localStorage 恢复表单数据和历史记录
  useEffect(() => {
    const savedFormData = localStorage.getItem('crawlQueryFormData');
    if (savedFormData) {
      try {
        const formData = JSON.parse(savedFormData);
        form.setFieldsValue(formData);
      } catch (error) {
        console.error('Failed to restore form data:', error);
      }
    }

    // 恢复历史记录
    const savedHistory = localStorage.getItem('crawlQueryHistory');
    if (savedHistory) {
      try {
        const history = JSON.parse(savedHistory);
        setQueryHistory(history);
      } catch (error) {
        console.error('Failed to restore query history:', error);
      }
    }
  }, [form]);

  // 保存表单数据到 localStorage
  const saveFormData = () => {
    const formData = form.getFieldsValue();
    localStorage.setItem('crawlQueryFormData', JSON.stringify(formData));
  };

  useEffect(() => {
    let interval: ReturnType<typeof setInterval>;
    if (polling && currentPollingTaskId) {
      // ⚡ 缩短轮询间隔从2000ms改为200ms,提升响应速度
      interval = setInterval(() => {
        loadTaskStatus(currentPollingTaskId);
      }, 200); // 从2000ms改为200ms
    }
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [polling, currentPollingTaskId]);

  // 计时器 - 动态显示消耗时间
  useEffect(() => {
    let timer: ReturnType<typeof setInterval>;
    if (polling && startTime > 0) {
      timer = setInterval(() => {
        setElapsedTime(Date.now() - startTime);
      }, 100); // 每100ms更新一次
    }
    return () => {
      if (timer) clearInterval(timer);
    };
  }, [polling, startTime]);

  const loadTaskStatus = async (taskId: string) => {
    try {
      const response: any = await crawlApi.getTask(taskId);
      const apiData = response.data;
      
      // 转换为前端格式
      const task: CrawlTask = {
        task_id: apiData.taskId,
        status: apiData.status,
        total_count: apiData.totalCount,
        success_count: apiData.results?.filter((r: any) => r.status === 'success').length || 0,
        failed_count: apiData.results?.filter((r: any) => r.status === 'failed').length || 0,
        created_at: new Date().toISOString(),
        results: apiData.results?.map((r: any) => ({
          model_number: r.modelNumber,
          status: r.status,
          data: r.data,
          error_message: r.errorMessage,
          duration: r.duration,
          retry_count: r.retryCount,
        })) || [],
      };
      
      // 更新结果列表中的任务
      setQueryResults(prev => {
        const index = prev.findIndex(t => t.task_id === taskId);
        if (index >= 0) {
          const newResults = [...prev];
          newResults[index] = task;
          return newResults;
        }
        return [...prev, task];
      });

      if (task.status === 'completed' || task.status === 'failed') {
        setPolling(false);
        
        // ✅ 使用前端实时计时,与进度条保持一致
        const duration = Date.now() - startTime;
        setTotalDuration(duration);
        setElapsedTime(duration);
        
        // 添加到历史记录
        const updatedHistory = [task, ...queryHistory].slice(0, 10); // 保留最近10条
        setQueryHistory(updatedHistory);
        localStorage.setItem('crawlQueryHistory', JSON.stringify(updatedHistory));
        
        message.success('抓取任务已完成');
      }
    } catch (error: any) {
      console.error('获取任务状态失败:', error);
    }
  };

  const handleSubmit = async (values: any) => {
    const { models_input, use_cache, retry_times } = values;
    const models = models_input
      .split('\n')
      .map((m: string) => m.trim())
      .filter((m: string) => m);

    if (models.length === 0) {
      message.error('请输入至少一个型号');
      return;
    }

    // 保存表单数据
    saveFormData();

    setLoading(true);
    const queryStartTime = Date.now();
    setStartTime(queryStartTime);
    setElapsedTime(0);
    setTotalDuration(0);
    
    try {
      const response: any = await crawlApi.submit(models, use_cache, retry_times || 3);
      message.success('任务已提交');
      
      const newTask: CrawlTask = {
        task_id: response.data.taskId,
        status: 'running' as 'running',
        total_count: response.data.totalCount,
        success_count: 0,
        failed_count: 0,
        created_at: new Date().toISOString(),
      };
      
      // 添加到结果列表头部
      setQueryResults(prev => [newTask, ...prev]);
      setCurrentPollingTaskId(response.data.taskId);
      setPolling(true);
    } catch (error: any) {
      message.error('提交失败: ' + error.message);
    } finally {
      setLoading(false);
    }
  };

  const refreshTask = (taskId: string) => {
    loadTaskStatus(taskId);
  };

  const clearResults = () => {
    setQueryResults([]);
    setPolling(false);
    setCurrentPollingTaskId('');
    setStartTime(0);
    setElapsedTime(0);
    setTotalDuration(0);
    message.success('已清除查询结果');
  };

  const clearAll = () => {
    form.resetFields();
    localStorage.removeItem('crawlQueryFormData');
    clearResults();
    message.success('已清除所有数据');
  };

  const clearHistory = () => {
    setQueryHistory([]);
    localStorage.removeItem('crawlQueryHistory');
    message.success('已清除历史记录');
  };

  const loadHistoryTask = async (taskId: string) => {
    try {
      const response: any = await crawlApi.getTask(taskId);
      const task = response.data;
      
      // 检查是否已存在
      const exists = queryResults.some(t => t.task_id === taskId);
      if (!exists) {
        setQueryResults(prev => [task, ...prev]);
      }
      
      message.success('已加载历史任务');
    } catch (error: any) {
      message.error('加载失败: ' + error.message);
    }
  };

  const getStatusTag = (status: string) => {
    const statusMap: Record<string, { color: string; text: string }> = {
      pending: { color: 'default', text: '等待中' },
      running: { color: 'processing', text: '运行中' },
      completed: { color: 'success', text: '已完成' },
      failed: { color: 'error', text: '失败' },
      success: { color: 'success', text: '成功' },
    };
    const { color, text } = statusMap[status] || statusMap.pending;
    return <Tag color={color}>{text}</Tag>;
  };

  // 为结果行获取状态标签
  const getResultStatusTag = (record: any) => {
    // 如果有error_message，显示失败
    if (record.error_message && record.error_message !== '') {
      return <Tag color="error">失败</Tag>;
    }
    // 如果有data且data不为空，显示成功
    if (record.data && Object.keys(record.data).length > 0) {
      const hasData = Object.values(record.data).some(v => v && v !== '');
      if (hasData) {
        return <Tag color="success">成功</Tag>;
      }
    }
    // 如果status是success，显示成功
    if (record.status === 'success') {
      return <Tag color="success">成功</Tag>;
    }
    // 其他情况根据status字段判断
    return getStatusTag(record.status);
  };

  const columns = [
    {
      title: '型号',
      dataIndex: 'model_number',
      key: 'model_number',
      width: 150,
    },
    {
      title: '状态',
      dataIndex: 'status',
      key: 'status',
      width: 100,
      render: (_: string, record: any) => getResultStatusTag(record),
    },
    {
      title: '价格',
      key: 'price',
      width: 120,
      render: (_: any, record: any) => record.data?.price || '-',
    },
    {
      title: '库存',
      key: 'stock',
      width: 100,
      render: (_: any, record: any) => record.data?.stock || '-',
    },
    {
      title: '交期',
      key: 'delivery',
      width: 100,
      render: (_: any, record: any) => record.data?.delivery || '-',
    },
    {
      title: '单项耗时',
      dataIndex: 'duration',
      key: 'duration',
      width: 120,
      render: (duration: number) => {
        if (!duration) return '-';
        return (
          <span style={{ color: duration > 3000 ? '#ff4d4f' : '#52c41a' }}>
            {duration.toLocaleString()}ms
          </span>
        );
      },
    },
    {
      title: '尝试次数',
      dataIndex: 'retry_count',
      key: 'retry_count',
      width: 100,
      render: (retryCount: number) => {
        if (!retryCount) return '1';
        return (
          <span style={{ color: retryCount > 1 ? '#faad14' : '#52c41a' }}>
            {retryCount}
          </span>
        );
      },
    },
    {
      title: '错误信息',
      dataIndex: 'error_message',
      key: 'error_message',
      render: (text: string) => text || '-',
    },
  ];

  return (
    <Space direction="vertical" size="small" style={{ width: '100%' }}>
      <Card title="价格查询" size="small">
        <Form 
          form={form} 
          onFinish={handleSubmit} 
          layout="vertical"
          initialValues={{ use_cache: true, retry_times: 3 }}
          style={{ marginBottom: 0 }}
        >
          <Form.Item
            label="输入型号（每行一个）"
            name="models_input"
            rules={[{ required: true, message: '请输入元器件型号' }]}
            style={{ marginBottom: 12 }}
          >
            <TextArea
              rows={4}
              placeholder="例如：&#10;STM32F103&#10;ATmega328P&#10;ESP32-WROOM-32"
              onBlur={saveFormData}
            />
          </Form.Item>

          <Form.Item name="use_cache" label="缓存设置" style={{ marginBottom: 12 }}>
            <Select onChange={saveFormData}>
              <Option value={true}>使用缓存</Option>
              <Option value={false}>不使用缓存</Option>
            </Select>
          </Form.Item>

          <Form.Item 
            name="retry_times" 
            label="尝试次数" 
            style={{ marginBottom: 12 }}
            tooltip="当数据未包含数字时，最多尝试几次"
          >
            <Select onChange={saveFormData}>
              <Option value={1}>1次</Option>
              <Option value={2}>2次</Option>
              <Option value={3}>3次 (推荐)</Option>
              <Option value={4}>4次</Option>
              <Option value={5}>5次</Option>
            </Select>
          </Form.Item>

          <Form.Item style={{ marginBottom: 0 }}>
            <Space>
              <Button
                type="primary"
                htmlType="submit"
                icon={<SearchOutlined />}
                loading={loading}
                size="large"
              >
                开始查询
              </Button>
              <Button
                onClick={clearAll}
                size="large"
              >
                清空表单
              </Button>
            </Space>
          </Form.Item>
        </Form>
      </Card>

      {queryResults.length > 0 && (
        <Card
          title="查询结果"
          size="small"
          extra={
            <Button
              icon={<ClearOutlined />}
              onClick={clearResults}
              danger
              size="small"
            >
              清除结果
            </Button>
          }
        >
          {queryResults.map((task, taskIndex) => {
            const progress = Math.round(
              ((task.success_count + task.failed_count) / task.total_count) * 100
            );
            const isPolling = polling && task.task_id === currentPollingTaskId;
            
            return (
              <div key={task.task_id} style={{ marginBottom: taskIndex < queryResults.length - 1 ? 16 : 0 }}>
                <Space direction="vertical" style={{ width: '100%', marginBottom: 8 }}>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <div>
                      <strong>{new Date(task.created_at).toLocaleString()}</strong>
                      <span style={{ marginLeft: 16 }}>{getStatusTag(task.status)}</span>
                    </div>
                    <Button
                      size="small"
                      icon={<ReloadOutlined />}
                      onClick={() => refreshTask(task.task_id)}
                      loading={taskLoading}
                    >
                      刷新
                    </Button>
                  </div>
                  <div>
                    任务进度: {task.success_count + task.failed_count} / {task.total_count}
                  </div>
                  <Progress 
                    percent={progress} 
                    status={isPolling ? 'active' : 'normal'}
                    format={() => `${progress}%${isPolling ? ` | ${elapsedTime}ms` : ''}`}
                  />
                  <div>
                    成功: {task.success_count} | 失败: {task.failed_count}
                    {isPolling && totalDuration === 0 && (
                      <span style={{ marginLeft: 16, color: '#1890ff' }}>
                        已用时: {elapsedTime.toLocaleString()}ms ({(elapsedTime / 1000).toFixed(2)}s)
                      </span>
                    )}
                    {!isPolling && task.status === 'completed' && taskIndex === 0 && totalDuration > 0 && (
                      <span style={{ marginLeft: 16, color: '#52c41a', fontWeight: 'bold' }}>
                        任务总耗时: {totalDuration.toLocaleString()}ms ({(totalDuration / 1000).toFixed(2)}s)
                      </span>
                    )}
                  </div>
                </Space>

                <Table
                  columns={columns}
                  dataSource={task.results || []}
                  rowKey="model_number"
                  pagination={false}
                  size="small"
                />
                {taskIndex < queryResults.length - 1 && <div style={{ borderBottom: '1px solid #f0f0f0', margin: '16px 0' }} />}
              </div>
            );
          })}
        </Card>
      )}

      {queryHistory.length > 0 && (
        <Card 
          title="查询历史" 
          size="small"
          extra={
            <Button size="small" onClick={clearHistory}>
              清除历史
            </Button>
          }
        >
          <div style={{ maxHeight: '300px', overflowY: 'auto' }}>
            {queryHistory.map((task, index) => (
              <div 
                key={task.task_id || index}
                style={{
                  padding: '8px 12px',
                  marginBottom: '8px',
                  background: '#f5f5f5',
                  borderRadius: '4px',
                  cursor: 'pointer',
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
                onClick={() => loadHistoryTask(task.task_id)}
              >
                <div>
                  <span style={{ fontWeight: 'bold' }}>
                    {new Date(task.created_at).toLocaleString()}
                  </span>
                  <span style={{ marginLeft: 16 }}>
                    总数: {task.total_count} | 成功: {task.success_count} | 失败: {task.failed_count}
                  </span>
                </div>
                <div>
                  {getStatusTag(task.status)}
                </div>
              </div>
            ))}
          </div>
        </Card>
      )}
    </Space>
  );
};

export default CrawlQuery;
