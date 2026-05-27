import type { GeneratingSummary } from "@/types/workflow";

export const mockActiveWorkflows: GeneratingSummary[] = [
  {
    instanceId: "wf-a1b2c3d4",
    name: "Giấy khen học sinh - Đợt 1",
    recipeId: 1,
    status: "Running",
    createdAt: new Date(Date.now() - 3 * 60000).toISOString(),
  },
  {
    instanceId: "wf-e5f6g7h8",
    name: "Thẻ học sinh lớp 10A",
    recipeId: 2,
    status: "Running",
    createdAt: new Date(Date.now() - 8 * 60000).toISOString(),
  },
  {
    instanceId: "wf-i9j0k1l2",
    name: "Bảng điểm cuối kỳ",
    recipeId: 3,
    status: "Paused",
    createdAt: new Date(Date.now() - 15 * 60000).toISOString(),
  },
  {
    instanceId: "wf-m3n4o5p6",
    name: "Báo cáo tháng 4",
    recipeId: 5,
    status: "Error",
    createdAt: new Date(Date.now() - 32 * 60000).toISOString(),
  },
];

export const mockCompletedWorkflows: GeneratingSummary[] = [
  {
    instanceId: "wf-q7r8s9t0",
    name: "Giấy khen học sinh - Thử nghiệm",
    recipeId: 1,
    status: "Complete",
    createdAt: new Date(Date.now() - 2 * 3600000).toISOString(),
    completedAt: new Date(Date.now() - 1.8 * 3600000).toISOString(),
  },
  {
    instanceId: "wf-u1v2w3x4",
    name: "Thẻ học sinh - Lớp 10A1",
    recipeId: 2,
    status: "Complete",
    createdAt: new Date(Date.now() - 5 * 3600000).toISOString(),
    completedAt: new Date(Date.now() - 4.5 * 3600000).toISOString(),
  },
  {
    instanceId: "wf-y5z6a7b8",
    name: "Giấy khen - Đợt 2",
    recipeId: 1,
    status: "Cancelled",
    createdAt: new Date(Date.now() - 8 * 3600000).toISOString(),
    completedAt: new Date(Date.now() - 7 * 3600000).toISOString(),
  },
  {
    instanceId: "wf-c9d0e1f2",
    name: "Báo cáo tháng 3",
    recipeId: 5,
    status: "Error",
    createdAt: new Date(Date.now() - 24 * 3600000).toISOString(),
    completedAt: new Date(Date.now() - 23.5 * 3600000).toISOString(),
  },
  {
    instanceId: "wf-g3h4i5j6",
    name: "Bảng điểm HK1",
    recipeId: 3,
    status: "Complete",
    createdAt: new Date(Date.now() - 48 * 3600000).toISOString(),
    completedAt: new Date(Date.now() - 47 * 3600000).toISOString(),
  },
  {
    instanceId: "wf-k7l8m9n0",
    name: "Thẻ học sinh - Lớp 11B",
    recipeId: 2,
    status: "Complete",
    createdAt: new Date(Date.now() - 72 * 3600000).toISOString(),
    completedAt: new Date(Date.now() - 71 * 3600000).toISOString(),
  },
];
