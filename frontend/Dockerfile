# Simple Dockerfile for serving the static AuthServiceMock web app

FROM nginx:alpine

# Copy static files into Nginx’s default HTML directory
COPY . /usr/share/nginx/html

# Expose the default HTTP port
EXPOSE 80

# Start Nginx in the foreground
CMD ["nginx", "-g", "daemon off;"]
