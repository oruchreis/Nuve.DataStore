namespace Nuve.DataStore.Redis
{
    public static class LuaCommands
    {
        //http://stackoverflow.com/questions/8899111/get-the-index-of-an-item-by-value-in-a-redis-list
        public const string IndexOf = @"local key = KEYS[1]
local obj = ARGV[1]
local items = redis.call('lrange', key, 0, -1)
for i=1,#items do
    if items[i] == obj then
        return i - 1
    end
end
return -1";

        // Applies the expiration only when the key exists without one, so an existing TTL is never extended.
        public const string ExpireWhenMissing = @"if redis.call('PTTL', KEYS[1]) == -1 then
    return redis.call('PEXPIRE', KEYS[1], ARGV[1])
end
return 0";

        // Writes the value keeping any remaining TTL, and applies the given expiration only when none exists.
        // SET on its own would clear the current TTL, which would turn a write into an implicit expiration reset.
        public const string SetKeepingTtl = @"local key = KEYS[1]
local value = ARGV[1]
local initialExpire = ARGV[2]
local skipWhenExists = ARGV[3] == '1'
if skipWhenExists and redis.call('EXISTS', key) == 1 then
    return 0
end
local pttl = redis.call('PTTL', key)
redis.call('SET', key, value)
if pttl > 0 then
    redis.call('PEXPIRE', key, pttl)
else
    redis.call('PEXPIRE', key, initialExpire)
end
return 1";

    }
}
