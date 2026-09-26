export interface ExamRegistrationDto {
  id: number;
  fullName: string;
  idCardNumber: string;
  phoneNumber: string;
  email: string;
  workUnit?: string;
  major?: string;
  departmentId?: number;
  departmentName?: string;
  examPurpose?: string;
  status: string;
  registrationDate: string;
  notes?: string;
}

export interface CreateExamRegistrationDto {
  fullName: string;
  idCardNumber: string;
  phoneNumber: string;
  email: string;
  password: string;
  workUnit?: string;
  major?: string;
  departmentId?: number;
  examPurpose?: string;
}

export interface RejectDto {
  reason?: string;
}
