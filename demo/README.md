# rds-demo
rds-demo

## demo to test the api of both monolith and microservice rds-app and benchmark comparison.


### to run the demo:

```
npm install
npm run dev
```

 
both backends needs to be running:  
- run microservices backend with main branch - no changes needed.  
- run monolith backend with main-monolith branch with these changes:  
 on docker-compose.yml file:  
    - change port to 54402 ("54402:80")
    - comment out "depends on" in Reference.Data.Service
    - comment out cosmosEmulator and zipkin services to avoid conflict on docker




## video:

https://github.com/user-attachments/assets/6590382f-7062-4e37-86cb-88d5683d4de7

## images:

![a](./img/image1.png)
![a](./img/image2.png)
![a](./img/image3.png)
![a](./img/image4.png)
![a](./img/image5.png)
