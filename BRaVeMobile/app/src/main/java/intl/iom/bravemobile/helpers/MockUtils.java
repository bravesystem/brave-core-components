package intl.iom.bravemobile.helpers;

public final class MockUtils {


    public static String randomCode() {
        String letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        StringBuilder sb = new StringBuilder(4);
        java.util.Random rand = new java.util.Random();

        for (int i = 0; i < 4; i++) {
            sb.append(letters.charAt(rand.nextInt(letters.length())));
        }

        return sb.toString();
    }

    public static String randomRelationship(int age, String genderHoH, String genderMem)
    {
        int x =randomInRange(18, 69);

        if(age < x && x>50)
            return "Other";

        if(x< 17)
            return "Son/Daughter";

        if(!genderMem.equalsIgnoreCase(genderHoH))
            return "Spouse";

        return "Other";
    }

    public static String randomGender()
    {
        if(randomInRange(1, 2) == 2 )
            return "Female";

        return "Male";
    }

    public static boolean randomBoolean()
    {
        if(randomInRange(1, 2) == 2 )
            return true;

        return false;
    }

    public static int randomAge(int x, int y){
        return randomInRange(x, y);
    }


    public static int randomInRange(int x, int y) {
        java.util.Random rand = new java.util.Random();
        return rand.nextInt((y - x) + 1) + x;
    }

    //private static String img_1 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGP4//8/AAX+Av4N70a4AAAAAElFTkSuQmCC";
    private static String img_2="iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGNgYGAAAAAEAAH2FzhVAAAAAElFTkSuQmCC";

    private static String img_3="iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGP4z8AAAAMBAQDJ/pLvAAAAAElFTkSuQmCC";

    private static String img_4 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGNg+M8AAAICAQB7CYF4AAAAAElFTkSuQmCC";

    private static String img_1="iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAIAAACQd1PeAAAADElEQVR4nGNgYPgPAAEDAQAIicLsAAAAAElFTkSuQmCC";

    public static String randomImg(int pick)
    {
        switch (pick){
            case 1:return img_1;
            case 2: return img_2;
            case 3: return img_3;
            //case 4: return img_4;
            default: return img_4;
        }

    }



}
