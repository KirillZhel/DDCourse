namespace Api.Models
{
	public class MetadataModel
	{
		public Guid TempId { get; set; }
		public string Name { get; set; } = null!;// можно так решить проблему с конструкторами
		public string MimeType { get; set; } = null!;
		public long Size { get; set; }
	}
}
