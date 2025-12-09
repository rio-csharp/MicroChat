# MicroChat
# [中文](./README.zh-CN.md)

## Deploy with Docker

### 1. Build and Push Image (Recommended: GitHub Actions)

This project is configured with GitHub Actions. When you push to the master branch, it will automatically build and push the Docker image to Docker Hub.

To build and push manually, use the following commands:

```powershell
 # Build image
 $env:DOCKERHUB_USERNAME="your DockerHub username"
docker build -t $env:DOCKERHUB_USERNAME/microchat:latest .

 # Login to Docker Hub (recommended: use token)
 $env:DOCKERHUB_TOKEN="your DockerHub token"
docker login -u $env:DOCKERHUB_USERNAME -p $env:DOCKERHUB_TOKEN

 # Push image
 docker push $env:DOCKERHUB_USERNAME/microchat:latest
```

### 2. Run the Container

```powershell
docker run -d -p 3000:3000 --name microchat $env:DOCKERHUB_USERNAME/microchat:latest
```


### 3. Environment Variables

#### Main Environment Variables

You can configure the following parameters via environment variables (for Docker or cloud deployment):

- `Models`: Model list (e.g. gpt-4.1,gpt-4o,gemini-2.5-pro)
- `ApiBaseUrl`: API base URL (e.g. https://api.openai.com)
- `ApiKey`: Your API key
- `AccessKey`: Application access key


Set environment variables with the `-e` option when running Docker:

```shell
docker run -d -p 3000:3000 --name microchat \
	-e Models="gpt-4.1,gpt-4o,gemini-2.5-pro" \
	-e ApiBaseUrl="https://api.openai.com" \
	-e ApiKey="your API key" \
	-e AccessKey="your access key" \
	leiyao233/microchat:latest
```


To customize the port, set the `ASPNETCORE_URLS` environment variable:

```shell
docker run -d -p 8080:8080 --name microchat \
	-e ASPNETCORE_URLS="http://+:8080" \
	leiyao233/microchat:latest
```


---

For questions, please submit an Issue or contact the maintainer.
