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

    }
}
