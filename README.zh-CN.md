# MicroChat

## 使用 Docker 部署

### 1. 构建并推送镜像（推荐使用 GitHub Actions 自动化）

本项目已配置 GitHub Actions，推送到 master 分支后会自动构建并推送 Docker 镜像到 Docker Hub。

如需手动构建和推送，请参考以下命令：

```powershell
# 构建镜像
$env:DOCKERHUB_USERNAME="你的DockerHub用户名"
docker build -t $env:DOCKERHUB_USERNAME/microchat:latest .

# 登录 Docker Hub
# 推荐使用 token 登录
$env:DOCKERHUB_TOKEN="你的DockerHub Token"
docker login -u $env:DOCKERHUB_USERNAME -p $env:DOCKERHUB_TOKEN

# 推送镜像
 docker push $env:DOCKERHUB_USERNAME/microchat:latest
```

### 2. 运行容器

```powershell
docker run -d -p 3000:3000 --name microchat $env:DOCKERHUB_USERNAME/microchat:latest
```

- 默认端口为 **3000**，可通过 `-p` 参数映射到主机端口。
- 应用启动后可通过 `http://localhost:3000` 访问。

### 3. 环境变量

#### 主要环境变量

应用支持通过环境变量配置以下参数（可用于 Docker 或云部署）：

- `Models`：模型列表（如 gpt-4.1,gpt-4o,gemini-2.5-pro）
- `ApiBaseUrl`：API 基础地址（如 https://api.openai.com）
- `ApiKey`：你的 API 密钥
- `AccessKey`：应用访问密钥

在 Docker 运行时可用 `-e` 参数设置，例如：

```shell
docker run -d -p 3000:3000 --name microchat \
  -e Models="gpt-4.1,gpt-4o,gemini-2.5-pro" \
  -e ApiBaseUrl="https://api.openai.com" \
  -e ApiKey="你的API密钥" \
  -e AccessKey="你的访问密钥" \
  leiyao233/microchat:latest
```

如需自定义端口，可设置 `ASPNETCORE_URLS` 环境变量：

```shell
docker run -d -p 8080:8080 --name microchat \
  -e ASPNETCORE_URLS="http://+:8080" \
  leiyao233/microchat:latest
```

也可在云服务平台（如 Azure、AWS）通过环境变量面板配置。

---

如有问题请提交 Issue 或联系维护者。
