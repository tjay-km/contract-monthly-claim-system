using CMCS.Models;
using CMCS.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;

namespace CMCS.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadDirectory;

        public DocumentService(ApplicationDbContext context)
        {
            _context = context;
            _uploadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            if (!Directory.Exists(_uploadDirectory))
            {
                Directory.CreateDirectory(_uploadDirectory);
            }
        }

        public async Task<string> SaveDocumentAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded.");

            // Create a unique filename
            string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            string filePath = Path.Combine(_uploadDirectory, uniqueFileName);

            // Save the file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return uniqueFileName;
        }

        public async Task<string> AddClaimDocument(ClaimDocument claimDocument, IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("No file was uploaded.");

            // Save the file and get the path
            string fileName = await SaveDocumentAsync(file);
            
            // Update the claim document with the file info
            claimDocument.FileName = fileName;
            claimDocument.FilePath = Path.Combine("uploads", fileName);
            claimDocument.DateUploaded = DateTime.UtcNow;

            // Save to database
            _context.ClaimDocuments.Add(claimDocument);
            await _context.SaveChangesAsync();

            return claimDocument.FilePath;
        }

        public async Task<List<ClaimDocument>> GetClaimDocuments(int claimId)
        {
            return await _context.ClaimDocuments
                .Where(x => x.ClaimId == claimId)
                .ToListAsync();
        }

        public async Task DeleteClaimDocument(int claimDocumentId)
        {
            var claimDocument = await _context.ClaimDocuments.FindAsync(claimDocumentId);
            if (claimDocument != null)
            {
                // Delete the physical file
                string filePath = Path.Combine(_uploadDirectory, claimDocument.FileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                _context.ClaimDocuments.Remove(claimDocument);
                await _context.SaveChangesAsync();
            }
        }
    }
}
