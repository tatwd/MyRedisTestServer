using MyRedisTestServer;

new RedisTestServer(6379).DebugMode(true).StartAsync().Wait();