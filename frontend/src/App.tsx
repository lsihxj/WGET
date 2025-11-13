import { Layout } from 'antd';
import CrawlQuery from './pages/CrawlQuery';

const { Header, Content } = Layout;

function App() {
  return (
    <Layout style={{ minHeight: '100vh' }}>
      <Header style={{ 
        background: '#fff', 
        padding: '0 24px',
        boxShadow: '0 2px 8px rgba(0,0,0,0.1)',
        display: 'flex',
        alignItems: 'center'
      }}>
        <h1 style={{ margin: 0, fontSize: '20px', fontWeight: 'bold' }}>
          WGet 价格查询系统
        </h1>
      </Header>
      <Content style={{ padding: '16px', background: '#f0f2f5' }}>
        <CrawlQuery />
      </Content>
    </Layout>
  );
}

export default App;
