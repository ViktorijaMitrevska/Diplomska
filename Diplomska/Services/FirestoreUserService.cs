using Diplomska.Models;
using Google.Cloud.Firestore;

public class FirestoreUserService
{
    private readonly FirestoreDb _db;

    public FirestoreUserService(FirestoreDb db)
    {
        _db = db;
    }

    public async Task AddUserAsync(AppUser user)
    {
        await _db.Collection("Users")
            .Document(user.UserId)
            .SetAsync(user);
    }

    public async Task<AppUser?> GetUserByEmailAsync(string email)
    {
        var snapshot = await _db.Collection("Users")
            .WhereEqualTo("Email", email)
            .Limit(1)
            .GetSnapshotAsync();

        if (!snapshot.Documents.Any())
            return null;

        return snapshot.Documents.First().ConvertTo<AppUser>();
    }

    public async Task<bool> OwnerExistsAsync()
    {
        var snapshot = await _db.Collection("Users")
            .WhereEqualTo("Role", "Owner")
            .Limit(1)
            .GetSnapshotAsync();

        return snapshot.Documents.Any();
    }

    public async Task<List<AppUser>> GetUsersByRoleAsync(string role)
    {
        var snapshot = await _db.Collection("Users")
            .WhereEqualTo("Role", role)
            .GetSnapshotAsync();

        return snapshot.Documents
            .Select(d => d.ConvertTo<AppUser>())
            .ToList();
    }

    public async Task<AppUser?> GetUserByIdAsync(string userId)
    {
        var snap = await _db.Collection("Users")
            .Document(userId)
            .GetSnapshotAsync();

        return snap.Exists ? snap.ConvertTo<AppUser>() : null;
    }
}
