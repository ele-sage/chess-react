# Dev
FROM node:22-alpine3.19

WORKDIR /app

COPY react/package.json react/package-lock.json ./react/
WORKDIR /app/react
RUN npm install

COPY react/. ./

EXPOSE 3000

CMD ["npm", "run", "start"]