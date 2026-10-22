using EduCore.Security.Encryption;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace EduCore.Data;

// EF builds a context type's model once and caches it for the process. The field-encryption
// converters are lambdas over the encryptor, so without this every later context would encrypt
// with whichever encryptor the first one happened to have. Keying the cache on the encryptor
// gives each one its own model: one in production (the singleton), and a correct one for any
// other in the same process -- a test double, or a rotation tool holding a second key set.
public sealed class EncryptorModelCacheKeyFactory : IModelCacheKeyFactory
{
    public object Create(DbContext context, bool designTime) =>
        context is AppDbContext app
            ? (context.GetType(), app.Encryptor, designTime)
            : (context.GetType(), designTime);
}
