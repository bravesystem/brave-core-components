package intl.iom.bravemobile.models.distributions;

import java.util.List;

import intl.iom.bravemobile.helpers.StringUtils;

public class Family {


    public String householdUuid;
    private String match_uuid;
    private String hohid;

    private int is_enrolled;
    private String uuid;
    private String type;
    private List<Member> members;

    public Family(String householdUuid, String hohid, String type, List<Member> members) {
        this.householdUuid = householdUuid;
        this.hohid = hohid;
        this.type = type;
        this.members = members;
    }

    public String getHouseholdUuid() { return householdUuid; }
    public String getHohid() { return hohid; }
    public String getType() { return type; }

    public String getUuid() { return uuid; }

    public void set_enrolled(int is_enrolled)
    {
        this.is_enrolled = is_enrolled;
    }

    public boolean isEnrolled()
    {
        return is_enrolled == 1;
    }

    public String getMatchedUuid() { return match_uuid; }
    public List<Member> getMembers() { return members; }

    public void setMatch_uuid(String uuid)
    {
        match_uuid = uuid;
    }


    public static class Member {
        private String uuid;
        private int memno;
        private String relationship;
        private String fullName;
        private String gender;
        private int age;
        private boolean has_biometric;
        private String photoB64;
        private String biometricB64;

        public Member(String uuid, int memno, String relationship, String fullName, String gender, int age) {
            this.uuid = uuid;
            this.memno = memno;
            this.relationship = relationship;
            this.fullName = fullName;
            this.gender = gender;
            this.age = age;
        }

        public Member(String uuid, int memno, String relationship, String fullName, String gender, int age, String photoB64, String biometricB64)
        {
            this(uuid, memno, relationship, fullName, gender, age);
            this.photoB64 = photoB64;
            this.biometricB64 = biometricB64;
            if(!StringUtils.isBlank(biometricB64))
                has_biometric= true;
        }

        public Member(String uuid, int memno, String relationship, String fullName, String gender, int age, String photoB64, boolean has_biometric)
        {
            this(uuid, memno, relationship, fullName, gender, age);
            this.photoB64 = photoB64;
            this.has_biometric= has_biometric;
        }

        public String getUuid() { return uuid; }
        public int getMemno() { return memno; }
        public String getRelationship() { return relationship; }
        public String getFullName() { return fullName; }
        public String getGender() { return gender; }
        public int getAge() { return age; }
        public String getPhotoB64() { return photoB64; }
        public String getBiometricB64() { return biometricB64; }

        public boolean isBiometricCollected()
        {
            //if(has_biometric)
            //return !StringUtils.isBlank(biometricB64);
            return has_biometric;
        }
    }

}
