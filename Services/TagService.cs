using System.Text.Json;
using ExpenseTracker.Models;

public class TagService
{
    private static readonly string DesktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    private static readonly string FolderPath = Path.Combine(DesktopPath, "LocalDB");
    private static readonly string TagsFilePath = Path.Combine(FolderPath, "tags.json");

    // Load all tags
    public List<Tag> LoadTags()
    {
        if (!File.Exists(TagsFilePath))
            return new List<Tag>(); // Return an empty list if no tags exist

        var json = File.ReadAllText(TagsFilePath);
        return JsonSerializer.Deserialize<List<Tag>>(json) ?? new List<Tag>();
    }

    public List<string> GetTagNames()
    {
        return LoadTags().Select(tag => tag.Name).ToList();
    }

    // Save tags
    public void SaveTags(List<Tag> tags)
    {
        EnsureDirectoryExists();

        var json = JsonSerializer.Serialize(tags, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(TagsFilePath, json);
    }

    // Add a new tag
    public void AddTag(string tagName)
    {
        var tags = LoadTags();

        // Ensure the tag name is unique
        if (!tags.Any(tag => tag.Name.Equals(tagName, StringComparison.OrdinalIgnoreCase)))
        {
            var newTag = new Tag
            {
                Id = tags.Count > 0 ? tags.Max(tag => tag.Id) + 1 : 1, // Increment ID
                Name = tagName
            };
            tags.Add(newTag);
            SaveTags(tags);
        }
    }

    // Delete a tag
    public void DeleteTag(int tagId)
    {
        var tags = LoadTags();
        var tagToRemove = tags.FirstOrDefault(tag => tag.Id == tagId);
        if (tagToRemove != null)
        {
            tags.Remove(tagToRemove);
            SaveTags(tags);
        }
    }

    // Ensure the directory for storage exists
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(FolderPath))
        {
            Directory.CreateDirectory(FolderPath);
        }
    }
}