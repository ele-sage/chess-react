# Dev
FROM node:22-alpine3.19

# COPY nginx.conf /etc/nginx/nginx.conf

WORKDIR /app

COPY package.json package-lock.json ./

RUN npm install

COPY . ./

EXPOSE 3000

CMD ["npm", "run", "dev"]