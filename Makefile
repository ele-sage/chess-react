start:
	docker compose up --build

stop:
	docker compose down
	yes | docker network prune
	yes | docker image prune

delete_volumes:
	make stop
	docker volume ls -q | xargs docker volume rm

prune:
	docker system prune --all --volumes

frontend:
	docker exec -it chess-frontend-1 sh

backend:
	docker exec -it chess-backend-1 bash

.PHONY: start stop prune frontend backend