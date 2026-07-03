FROM node:22-alpine AS build
WORKDIR /src
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
ARG VITE_MINI_SSO_ORIGIN=http://localhost:12080
ENV VITE_MINI_SSO_ORIGIN=$VITE_MINI_SSO_ORIGIN
RUN npm run build

FROM nginx:1.27-alpine AS final
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
