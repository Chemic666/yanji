using YanJi.Core;
namespace YanJi.Data;
public sealed partial class YanJiDatabase
{
    public void ConfigureDefaultProfile(string name,DateOnly? examDate,IReadOnlyCollection<string> enabledSubjects)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);if(enabledSubjects.Count==0)throw new ArgumentException("至少保留一个使用中的科目。");
        lock(_gate)
        {
            using var c=Open();using var tx=c.BeginTransaction();var all=Subjects(true);if(enabledSubjects.Any(id=>all.All(s=>s.Id!=id)))throw new ArgumentException("科目不存在。");
            var before=ExamProfiles().Single(p=>p.Id=="legacy-default");var after=before with{Name=name.Trim(),ExamDate=examDate};
            Exec(c,tx,"UPDATE exam_profiles SET name=$name,exam_date=$date WHERE id='legacy-default'",("$name",after.Name),("$date",after.ExamDate?.ToString("yyyy-MM-dd")));
            foreach(var subject in all){var archived=!enabledSubjects.Contains(subject.Id);if(subject.Archived==archived)continue;var changed=subject with{Archived=archived};Exec(c,tx,"UPDATE subjects SET archived=$a WHERE id=$id",("$a",archived),("$id",subject.Id));Audit(c,tx,"subject",subject.Id,0,"考试科目选择",Json(subject),Json(changed));}
            Audit(c,tx,"exam-profile",after.Id,0,"考试档案配置",Json(before),Json(after));Exec(c,tx,"INSERT INTO settings VALUES('onboarding-complete',$v) ON CONFLICT(id) DO UPDATE SET payload=excluded.payload",("$v",Json("true")));tx.Commit();
        }
    }
}
