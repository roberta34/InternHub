using System;
using System.Collections.Generic;
using System.Linq;
using InternHub.Api.Domain.InternshipCandidacies;
using Microsoft.EntityFrameworkCore;
namespace InternHub.Api.Infrastructure.Persistence;

public sealed class EfCoreInternshipCandidacyRepository(InternHubDbContext db) : IInternshipCandidacyRepository
{
       public bool Add(InternshipCandidacy candidacy)
       {
              ArgumentNullException.ThrowIfNull(candidacy);
              db.InternshipCandidacies.Add(candidacy);
              return db.SaveChanges() > 0;
       }

       public InternshipCandidacy? GetById(Guid id) =>
                db.InternshipCandidacies
                         .FirstOrDefault(c => c.Id == id);

       public IReadOnlyList<InternshipCandidacy> GetByStudent(Guid studentId) =>
           db.InternshipCandidacies
               .AsNoTracking()
               .Where(c => c.StudentId == studentId)
               .OrderByDescending(c => c.ApplicationDate)
               .ThenBy(c => c.Id)
               .ToList();

       public IReadOnlyList<InternshipCandidacy> GetByOffer(Guid offerId) =>
           db.InternshipCandidacies
               .AsNoTracking()
               .Where(c => c.OfferId == offerId)
               .OrderByDescending(c => c.ApplicationDate)
               .ThenBy(c => c.Id)
               .ToList();

       public bool HasActiveForStudentAndOffer(Guid studentId, Guid offerId) =>
           db.InternshipCandidacies.Any(c =>
               c.StudentId == studentId &&
               c.OfferId == offerId &&
               (c.Status == InternshipCandidacyStatus.Submitted ||
                c.Status == InternshipCandidacyStatus.Shortlisted ||
                c.Status == InternshipCandidacyStatus.Accepted ||
                c.Status == InternshipCandidacyStatus.Waitlisted));

       public int CountActiveForStudent(Guid studentId) =>
           db.InternshipCandidacies.Count(c =>
               c.StudentId == studentId &&
               (c.Status == InternshipCandidacyStatus.Submitted ||
                c.Status == InternshipCandidacyStatus.Shortlisted ||
                c.Status == InternshipCandidacyStatus.Accepted ||
                c.Status == InternshipCandidacyStatus.Waitlisted));

       public int CountAcceptedForOffer(Guid offerId) =>
           db.InternshipCandidacies.Count(c =>
               c.OfferId == offerId &&
               c.Status == InternshipCandidacyStatus.Accepted);

       public bool Update(InternshipCandidacy candidacy)
       {
           ArgumentNullException.ThrowIfNull(candidacy);

           if (!db.InternshipCandidacies.Any(c => c.Id == candidacy.Id))
           {
               return false;
           }

           db.InternshipCandidacies.Update(candidacy);
           return db.SaveChanges() > 0;
       }
}
