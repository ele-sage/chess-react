# Dev
FROM node:22-alpine3.19

# COPY nginx.conf /etc/nginx/nginx.conf

WORKDIR /app

COPY react/package.json ./react/
WORKDIR /app/react
RUN npm install

COPY react/. ./

EXPOSE 3000

CMD ["npm", "run", "dev"]

# Use an official Node runtime as a parent image
# FROM node:22-alpine3.19 AS build

# WORKDIR /app

# COPY react/package.json react/package-lock.json ./react/
# WORKDIR /app/react
# RUN npm install

# COPY react/. ./
# RUN npm run build

# # Stage 2: Serve the React application with Nginx
# FROM nginx:alpine

# COPY --from=build /app/react/build /usr/share/nginx/html
# COPY nginx.conf /etc/nginx/nginx.conf

# CMD ["nginx", "-g", "daemon off;"]