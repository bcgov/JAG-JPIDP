namespace jumwebapi.Models
{
    public class UserModel : BaseAuditable
    {
        public long UserId { get; set; }
        public bool IsDisabled { get; set; }
        public PersonModel Person { get; set; } = new PersonModel();
        public ParticipantModel Participant { get; set; } = new ParticipantModel();
        public IEnumerable<RoleModel> Roles { get; set; } = new List<RoleModel>();
        public IdentityProviderModel IdentityProvider { get; set; } = new IdentityProviderModel();
        public IEnumerable<AgencyAssignmentModel> AgencyAssignments { get; set; } = new List<AgencyAssignmentModel>();

    }
}
