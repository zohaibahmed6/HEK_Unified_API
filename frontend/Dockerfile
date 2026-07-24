# v1.1 spec follow-through Step 15: fully separate deployment from the API (confirmed architecture),
# calls the API purely over HTTP. Multi-stage: build the Vite bundle, then serve the static output
# with nginx - same "one door, correct address" principle as the rest of this project.

FROM node:20-alpine AS build
WORKDIR /src
ARG VITE_API_BASE_URL
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
COPY package.json package-lock.json ./
RUN npm ci
COPY . .
RUN npm run build

FROM nginx:1.27-alpine AS runtime
COPY --from=build /src/dist /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
