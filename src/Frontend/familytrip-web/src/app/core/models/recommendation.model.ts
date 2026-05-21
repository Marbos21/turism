export interface Recommendation {
  id: number;
  familyId: number;
  destinationId: number;
  destinationName: string;
  destinationCountry: string;
  destinationImageUrl: string;
  compatibilityScore: number;    // 0–100
  reasoning: string;
  highlights: RecommendationHighlight[];
  estimatedBudgetUsd: number;
  bestTravelMonths: number[];
  warnings?: string[];
  generatedAt: string;
}

export interface RecommendationHighlight {
  category: string;
  description: string;
  icon: string;
}

export interface GenerateRecommendationsRequest {
  familyId: number | string;
  budget?: number;
  preferredMonths?: number[];
  excludeDestinationIds?: number[];
}

export interface ToolCallTrace {
  tool: string;
  input: unknown;
  output: string;
  durationMs: number;
  isError: boolean;
}

export interface AgentMeta {
  isAiPowered: boolean;
  model?: string;
  turnsUsed: number;
  totalInputTokens: number;
  totalCachedTokens: number;
  totalOutputTokens: number;
  toolCalls: ToolCallTrace[];
}

export interface GenerateRecommendationsResult {
  recommendations: Recommendation[];
  agentMeta: AgentMeta | null;
}
